"""Shared LLM client factory for gskill-kollector pipeline.

Uses the GitHub Models API (https://models.inference.ai.azure.com) which is
included with GitHub Copilot Pro — no separate OpenAI subscription required.

Authentication priority:
1. GITHUB_TOKEN  — GitHub Copilot Pro / GitHub Actions (preferred)
2. OPENAI_API_KEY — direct OpenAI fallback for local development

Supported models (GitHub Models): gpt-4o, gpt-4o-mini, o1, o3-mini,
claude-sonnet-4-5, mistral-large, etc.
Full list: https://github.com/marketplace/models
"""

from __future__ import annotations

import os

import subprocess
import json
from typing import Any

# OpenAI SDK is imported lazily inside make_client to avoid a hard dependency
# when the local copilot CLI adapter is used.
import re

GITHUB_MODELS_BASE_URL = "https://models.inference.ai.azure.com"


def _extract_last_code_block(text: str) -> str | None:
    """Return the content of the last ```[lang] ... ``` block in text, or None."""
    matches = list(re.finditer(r"```(?:\w*)\n?(.*?)```", text, re.DOTALL))
    if matches:
        return matches[-1].group(1).strip()
    return None


class CopilotCliAdapter:
    """Minimal adapter that exposes a subset of the OpenAI client API
    used by the pipeline: chat.completions.create(...)

    It runs the local `copilot` CLI as a subprocess and parses JSON output.
    """

    def __init__(self, model: str | None = None):
        self.model = model

    class chat:
        class completions:
            @staticmethod
            def create(model: str, messages: list[dict[str, str]], temperature: float = 0.0, **kwargs) -> Any:
                res = CopilotCliAdapter.chat.create(model=model, messages=messages, temperature=temperature, **kwargs)
                # If the nested call returned a dict (from older code paths), normalize
                if isinstance(res, dict):
                    content = res.get('output') or res.get('choices', [{}])[0].get('message', {}).get('content') if isinstance(res, dict) else None
                    Message = type('Message', (), {'content': content})
                    Choice = type('Choice', (), {'message': Message()})
                    return type('R', (), {'choices': [Choice()]})()
                return res

        @staticmethod
        def _extract_last_json_block(text: str):
            # Prefer explicit ```json code blocks
            json_blocks = list(re.finditer(r"```json\n?(.*?)```", text, re.DOTALL))
            if json_blocks:
                try:
                    return json.loads(json_blocks[-1].group(1))
                except Exception:
                    pass
            # Fallback: try any code block that parses as JSON
            code_blocks = list(re.finditer(r"```(?:\w*)\n?(.*?)```", text, re.DOTALL))
            for m in reversed(code_blocks):
                try:
                    return json.loads(m.group(1))
                except Exception:
                    continue
            # Last resort: try parsing entire output as JSON
            try:
                return json.loads(text)
            except Exception:
                return None

        @staticmethod
        def _run_copilot(model: str, messages: list[dict[str, str]], temperature: float) -> dict[str, Any]:
            # Build a simple prompt by concatenating system + user messages
            try:
                prompt_parts = []
                for m in messages:
                    role = m.get('role')
                    content = m.get('content')
                    prompt_parts.append(f"[{role}] {content}")
                prompt_text = "\n\n".join(prompt_parts)
                subcmd = "/model chat"
                if model:
                    subcmd += f" --model {model}"
                subcmd += f" -p {json.dumps(prompt_text)}"
                cmd = ["copilot", "-i", subcmd]
                proc = subprocess.run(
                    cmd,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    check=False,
                )
            except FileNotFoundError as e:
                raise RuntimeError("copilot CLI not found on PATH") from e

            if proc.returncode != 0:
                raise RuntimeError(f"copilot CLI failed: {proc.stderr.decode('utf-8').strip()}")

            text_out = proc.stdout.decode("utf-8")
            parsed = CopilotCliAdapter.chat._extract_last_json_block(text_out)
            if parsed is not None:
                # Normalize parsed JSON into OpenAI-like choices
                choices = []
                if isinstance(parsed, list):
                    for item in parsed:
                        if isinstance(item, dict):
                            content = item.get('message', {}).get('content') if item.get('message') else item.get('content') or json.dumps(item)
                        else:
                            content = str(item)
                        choices.append({'message': {'content': content}})
                elif isinstance(parsed, dict):
                    if isinstance(parsed.get('choices'), list):
                        for c in parsed['choices']:
                            if isinstance(c, dict):
                                content = c.get('message', {}).get('content') or c.get('text') or json.dumps(c)
                            else:
                                content = str(c)
                            choices.append({'message': {'content': content}})
                    else:
                        choices.append({'message': {'content': json.dumps(parsed)}})
                return {'choices': choices}

            cleaned = _extract_last_code_block(text_out) or text_out.strip()
            return {"output": cleaned}

        @staticmethod
        def create(model: str, messages: list[dict[str, str]], temperature: float = 0.0, **kwargs) -> Any:
            out = CopilotCliAdapter.chat._run_copilot(model, messages, temperature)
            # If we returned parsed choices, build objects matching .choices[0].message.content
            if isinstance(out, dict) and 'choices' in out:
                Message = type("Message", (), {"__init__": lambda self, content: setattr(self, "content", content)})
                Choice = type("Choice", (), {"__init__": lambda self, content: setattr(self, "message", Message(content))})
                r = type("R", (), {})()
                r.choices = [Choice(ch.get('message', {}).get('content')) for ch in out['choices']]
                return r

            # Fallback: previous behavior
            message_content = None
            if isinstance(out, dict):
                if "choices" in out and out["choices"]:
                    first = out["choices"][0]
                    if isinstance(first, dict):
                        message_content = first.get("message", {}).get("content") or first.get("text")
                    else:
                        message_content = str(first)
                elif "output" in out:
                    message_content = out["output"]
                else:
                    message_content = json.dumps(out)
            else:
                message_content = str(out)

            Message = type("Message", (), {"__init__": lambda self, content: setattr(self, "content", content)})
            Choice = type("Choice", (), {"__init__": lambda self, content: setattr(self, "message", Message(content))})
            r = type("R", (), {})()
            r.choices = [Choice(message_content)]
            return r


def make_client() -> Any:
    """Return an OpenAI-SDK-compatible client or CopilotCliAdapter when no API keys.

    Priority:
    1. GITHUB_TOKEN env var -> use GitHub Models via OpenAI client with base_url
    2. OPENAI_API_KEY env var -> use OpenAI client directly
    3. gh CLI token -> use GitHub Models via OpenAI client with base_url
    4. fallback to local `copilot` CLI adapter
    """
    # Allow forcing the local copilot CLI adapter via env var USE_COPILOT_CLI=1
    if os.environ.get("USE_COPILOT_CLI", "").lower() in ("1", "true", "yes"):
        return CopilotCliAdapter()
    gh_token = os.environ.get("GITHUB_TOKEN")
    if not gh_token:
        # Try to get token from gh CLI
        try:
            result = subprocess.run(
                ["gh", "auth", "token"],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                check=False,
            )
            if result.returncode == 0:
                gh_token = result.stdout.decode("utf-8").strip()
        except FileNotFoundError:
            pass

    if gh_token:
        try:
            from openai import OpenAI
        except Exception as e:
            raise RuntimeError("openai SDK is required to use GITHUB_TOKEN path") from e
        return OpenAI(base_url=GITHUB_MODELS_BASE_URL, api_key=gh_token)

    openai_key = os.environ.get("OPENAI_API_KEY")
    if openai_key:
        try:
            from openai import OpenAI
        except Exception as e:
            raise RuntimeError("openai SDK is required to use OPENAI_API_KEY path") from e
        return OpenAI(api_key=openai_key)

    # last resort: use copilot CLI adapter
    return CopilotCliAdapter()

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

from openai import OpenAI

GITHUB_MODELS_BASE_URL = "https://models.inference.ai.azure.com"


def make_client() -> OpenAI:
    """Return an OpenAI-SDK-compatible client.

    Prefers GitHub Models API (Copilot Pro) over OpenAI direct.
    Raises RuntimeError if neither credential is available.
    """
    gh_token = os.environ.get("GITHUB_TOKEN")
    if gh_token:
        return OpenAI(base_url=GITHUB_MODELS_BASE_URL, api_key=gh_token)

    openai_key = os.environ.get("OPENAI_API_KEY")
    if openai_key:
        return OpenAI(api_key=openai_key)

    raise RuntimeError(
        "No LLM credentials found. Set GITHUB_TOKEN (Copilot Pro) "
        "or OPENAI_API_KEY in your environment."
    )

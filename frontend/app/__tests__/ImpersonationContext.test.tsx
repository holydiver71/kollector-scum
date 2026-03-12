import React from 'react';
import { renderHook, act, waitFor } from '@testing-library/react';
import { ImpersonationProvider, useImpersonation } from '../contexts/ImpersonationContext';
import { useRouter } from 'next/navigation';

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <ImpersonationProvider>{children}</ImpersonationProvider>
);

describe('ImpersonationContext', () => {
  let mockPush: jest.Mock;

  beforeEach(() => {
    localStorage.clear();
    jest.clearAllMocks();
    mockPush = jest.fn();
    (useRouter as jest.Mock).mockReturnValue({
      push: mockPush,
      replace: jest.fn(),
      back: jest.fn(),
      refresh: jest.fn(),
      prefetch: jest.fn().mockResolvedValue(undefined),
    });
  });

  it('initialState_withNoLocalStorage_isImpersonatingIsFalse', () => {
    const { result } = renderHook(() => useImpersonation(), { wrapper });
    expect(result.current.isImpersonating).toBe(false);
    expect(result.current.impersonatedUserId).toBeNull();
    expect(result.current.impersonatedUserEmail).toBeNull();
    expect(result.current.impersonatedUserDisplayName).toBeNull();
  });

  it('initialState_withExistingLocalStorageKeys_hydratesState', async () => {
    localStorage.setItem('impersonation_userId', 'user-123');
    localStorage.setItem('impersonation_email', 'user@example.com');
    localStorage.setItem('impersonation_displayName', 'John Doe');

    const { result } = renderHook(() => useImpersonation(), { wrapper });

    await waitFor(() => {
      expect(result.current.isImpersonating).toBe(true);
    });
    expect(result.current.impersonatedUserId).toBe('user-123');
    expect(result.current.impersonatedUserEmail).toBe('user@example.com');
    expect(result.current.impersonatedUserDisplayName).toBe('John Doe');
  });

  it('startImpersonation_setsAllThreeLocalStorageKeys', () => {
    const setItemSpy = jest.spyOn(Storage.prototype, 'setItem');
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.startImpersonation({ userId: 'user-1', email: 'user@example.com', displayName: 'John Doe' });
    });

    expect(setItemSpy).toHaveBeenCalledWith('impersonation_userId', 'user-1');
    expect(setItemSpy).toHaveBeenCalledWith('impersonation_email', 'user@example.com');
    expect(setItemSpy).toHaveBeenCalledWith('impersonation_displayName', 'John Doe');
  });

  it('startImpersonation_setsIsImpersonatingTrue', () => {
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.startImpersonation({ userId: 'user-1', email: 'user@example.com' });
    });

    expect(result.current.isImpersonating).toBe(true);
    expect(result.current.impersonatedUserId).toBe('user-1');
    expect(result.current.impersonatedUserEmail).toBe('user@example.com');
  });

  it('startImpersonation_firesAuthChangedEvent', () => {
    const dispatchSpy = jest.spyOn(window, 'dispatchEvent');
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.startImpersonation({ userId: 'user-1', email: 'user@example.com' });
    });

    expect(dispatchSpy).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'authChanged' })
    );
  });

  it('startImpersonation_navigatesToDashboard', () => {
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.startImpersonation({ userId: 'user-1', email: 'user@example.com' });
    });

    // Navigation includes userId query param so SSR can honour impersonation
    expect(mockPush).toHaveBeenCalledWith('/dashboard?userId=user-1');
  });

  it('endImpersonation_clearsAllImpersonationLocalStorageKeys', () => {
    const removeItemSpy = jest.spyOn(Storage.prototype, 'removeItem');
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.startImpersonation({ userId: 'user-1', email: 'user@example.com', displayName: 'John' });
    });
    act(() => {
      result.current.endImpersonation();
    });

    expect(removeItemSpy).toHaveBeenCalledWith('impersonation_userId');
    expect(removeItemSpy).toHaveBeenCalledWith('impersonation_email');
    expect(removeItemSpy).toHaveBeenCalledWith('impersonation_displayName');
  });

  it('endImpersonation_setsIsImpersonatingFalse', () => {
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.startImpersonation({ userId: 'user-1', email: 'user@example.com' });
    });
    act(() => {
      result.current.endImpersonation();
    });

    expect(result.current.isImpersonating).toBe(false);
    expect(result.current.impersonatedUserId).toBeNull();
    expect(result.current.impersonatedUserEmail).toBeNull();
  });

  it('endImpersonation_firesAuthChangedEvent', () => {
    const dispatchSpy = jest.spyOn(window, 'dispatchEvent');
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.endImpersonation();
    });

    expect(dispatchSpy).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'authChanged' })
    );
  });

  it('endImpersonation_navigatesToAdminPage', () => {
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.endImpersonation();
    });

    expect(mockPush).toHaveBeenCalledWith('/admin');
  });

  it('endImpersonation_doesNotClearUnrelatedLocalStorageKeys', () => {
    localStorage.setItem('auth_token', 'my-token');
    const removeItemSpy = jest.spyOn(Storage.prototype, 'removeItem');
    const { result } = renderHook(() => useImpersonation(), { wrapper });

    act(() => {
      result.current.endImpersonation();
    });

    expect(removeItemSpy).not.toHaveBeenCalledWith('auth_token');
  });
});

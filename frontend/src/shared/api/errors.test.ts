import { describe, expect, it } from 'vitest';
import { AxiosError, AxiosHeaders } from 'axios';
import { getApiErrorMessage, getApiErrorStatus } from './errors';

function axiosErrorWith(status: number, data: unknown): AxiosError {
  const config = { headers: new AxiosHeaders() };
  return new AxiosError('Request failed', 'ERR_BAD_REQUEST', config, null, {
    status,
    statusText: '',
    headers: {},
    config,
    data,
  });
}

describe('getApiErrorStatus', () => {
  it('returns the HTTP status of an axios error', () => {
    expect(getApiErrorStatus(axiosErrorWith(404, {}))).toBe(404);
  });

  it('returns undefined for non-axios errors', () => {
    expect(getApiErrorStatus(new Error('boom'))).toBeUndefined();
  });
});

describe('getApiErrorMessage', () => {
  it('extracts the problem details title', () => {
    const error = axiosErrorWith(409, { status: 409, title: 'Ce bien a déjà un bail actif.' });
    expect(getApiErrorMessage(error, 'fallback')).toBe('Ce bien a déjà un bail actif.');
  });

  it('falls back when there is no title', () => {
    expect(getApiErrorMessage(axiosErrorWith(500, {}), 'fallback')).toBe('fallback');
    expect(getApiErrorMessage(new Error('boom'), 'fallback')).toBe('fallback');
  });
});

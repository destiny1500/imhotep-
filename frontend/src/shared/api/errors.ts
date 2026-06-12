import { isAxiosError } from 'axios';

/** HTTP status of an API error, if the error is an axios error with a response. */
export function getApiErrorStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined;
}

/**
 * Extracts the RFC 7807 problem `title` returned by the API,
 * falling back to the provided message.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const data = error.response?.data as { title?: unknown } | undefined;
    if (data && typeof data.title === 'string' && data.title.length > 0) {
      return data.title;
    }
  }
  return fallback;
}

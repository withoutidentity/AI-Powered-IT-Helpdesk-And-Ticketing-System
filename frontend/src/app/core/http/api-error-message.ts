import { HttpErrorResponse } from '@angular/common/http';

export interface ApiErrorMessageOptions {
  fallback: string;
  network?: string;
  validation?: string;
  unauthorized?: string;
  forbidden?: string;
  notFound?: string;
  conflict?: string;
  rateLimited?: string;
  server?: string;
}

export function getApiErrorMessage(error: unknown, options: ApiErrorMessageOptions | string): string {
  const config = typeof options === 'string' ? { fallback: options } : options;

  if (!(error instanceof HttpErrorResponse)) {
    return config.fallback;
  }

  const problemMessage = getProblemMessage(error);

  if (error.status === 0) {
    return config.network ?? 'Cannot reach the API server. Start the backend and try again.';
  }

  if (error.status === 400) {
    return problemMessage ?? config.validation ?? 'The submitted data is invalid.';
  }

  if (error.status === 401) {
    return config.unauthorized ?? 'Your session has expired. Sign in again.';
  }

  if (error.status === 403) {
    return config.forbidden ?? 'You do not have permission to perform this action.';
  }

  if (error.status === 404) {
    return problemMessage ?? config.notFound ?? 'The requested resource was not found.';
  }

  if (error.status === 409) {
    return problemMessage ?? config.conflict ?? 'The request conflicts with the current state.';
  }

  if (error.status === 429) {
    return problemMessage ?? config.rateLimited ?? 'Too many requests. Wait a moment and try again.';
  }

  if (error.status >= 500) {
    return config.server ?? 'The server encountered an error. Try again later.';
  }

  return problemMessage ?? config.fallback;
}

function getProblemMessage(error: HttpErrorResponse): string | null {
  const body = error.error as { detail?: unknown; title?: unknown } | null | undefined;

  if (typeof body?.detail === 'string' && body.detail.trim()) {
    return body.detail;
  }

  if (typeof body?.title === 'string' && body.title.trim()) {
    return body.title;
  }

  return null;
}

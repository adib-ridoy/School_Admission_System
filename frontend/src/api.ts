export type AdmissionClass = {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
};

export type Hold = {
  holdId: string;
  classId: string;
  studentId: string;
  status: string;
  expiresAt: string;
};

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';

async function request<T>(path: string, options: RequestInit = {}, studentId?: string): Promise<T> {
  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');
  if (studentId) {
    headers.set('X-Student-Id', studentId);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers });
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { error?: string } | null;
    throw new Error(body?.error ?? `Request failed with status ${response.status}.`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function listClasses() {
  return request<AdmissionClass[]>('/classes');
}

export function createClass(name: string, totalSeats: number) {
  return request<AdmissionClass>('/classes', {
    method: 'POST',
    body: JSON.stringify({ name, totalSeats })
  });
}

export function createHold(classId: string, studentId: string) {
  return request<Hold>(`/classes/${classId}/holds`, { method: 'POST' }, studentId);
}

export function listMyHolds(studentId: string) {
  return request<Hold[]>('/me/holds', {}, studentId);
}

export function confirmHold(holdId: string, studentId: string) {
  return request<Hold>(`/holds/${holdId}/confirm`, { method: 'POST' }, studentId);
}

export function cancelHold(holdId: string, studentId: string) {
  return request<void>(`/holds/${holdId}`, { method: 'DELETE' }, studentId);
}

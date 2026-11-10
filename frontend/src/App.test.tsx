import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

describe('School Admission System', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url.endsWith('/classes')) {
        return new Response(JSON.stringify([
          {
            id: 'c_123',
            name: 'Class 6 - Morning Shift',
            totalSeats: 120,
            heldSeats: 15,
            confirmedSeats: 80,
            availableSeats: 25
          }
        ]), { status: 200, headers: { 'Content-Type': 'application/json' } });
      }

      return new Response(JSON.stringify([]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      });
    }));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders class availability from the backend', async () => {
    render(<App />);

    expect(await screen.findByText('Class 6 - Morning Shift')).toBeInTheDocument();
    expect(screen.getByText('25')).toBeInTheDocument();
    expect(screen.getByText('available seats')).toBeInTheDocument();
  });

  it('requires a student ID before enabling seat holding', async () => {
    render(<App />);

    const holdButton = await screen.findByRole('button', { name: 'Enter student ID first' });
    expect(holdButton).toBeDisabled();

    fireEvent.change(screen.getByPlaceholderText('Enter your student ID'), {
      target: { value: 'student-001' }
    });

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Hold a seat' })).toBeEnabled()
    );
  });
});

import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  AdmissionClass,
  Hold,
  cancelHold,
  confirmHold,
  createClass,
  createHold,
  listClasses,
  listMyHolds
} from './api';

const studentStorageKey = 'school-admission-student-id';

function App() {
  const [studentId, setStudentId] = useState(
    () => localStorage.getItem(studentStorageKey) ?? ''
  );
  const [classes, setClasses] = useState<AdmissionClass[]>([]);
  const [holds, setHolds] = useState<Hold[]>([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      const classList = await listClasses();
      setClasses(classList);
      if (studentId.trim()) {
        setHolds(await listMyHolds(studentId.trim()));
      }
      setError('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load data.');
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const saveStudentId = (value: string) => {
    setStudentId(value);
    localStorage.setItem(studentStorageKey, value);
  };

  const runAction = async (action: () => Promise<unknown>, successMessage: string) => {
    try {
      await action();
      setMessage(successMessage);
      setError('');
      await refresh();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The operation failed.');
    }
  };

  return (
    <main className="shell">
      <header className="hero">
        <p className="eyebrow">School Admission System</p>
        <h1>Reserve a seat with confidence.</h1>
        <p className="intro">
          Browse available classes, hold a seat for a limited time, and confirm
          your admission from one simple dashboard.
        </p>
        <label className="student-field">
          <span>Student ID</span>
          <input
            value={studentId}
            onChange={(event) => saveStudentId(event.target.value)}
            placeholder="Enter your student ID"
          />
        </label>
      </header>

      <section className="content">
        {error && <div className="alert error">{error}</div>}
        {message && <div className="alert success">{message}</div>}
        <CreateClassForm onCreated={() => void refresh()} />

        <div className="section-heading">
          <div>
            <p className="eyebrow">Availability</p>
            <h2>Classes</h2>
          </div>
          <button className="secondary-button" onClick={() => void refresh()}>
            Refresh
          </button>
        </div>

        {loading ? (
          <p className="empty-state">Loading classes...</p>
        ) : classes.length === 0 ? (
          <p className="empty-state">No classes have been created yet.</p>
        ) : (
          <div className="class-grid">
            {classes.map((admissionClass) => (
              <ClassCard
                key={admissionClass.id}
                admissionClass={admissionClass}
                studentId={studentId}
                onHold={() =>
                  runAction(
                    () => createHold(admissionClass.id, studentId.trim()),
                    'Seat held successfully.'
                  )
                }
              />
            ))}
          </div>
        )}

        <div className="section-heading applications-heading">
          <div>
            <p className="eyebrow">Your activity</p>
            <h2>My applications</h2>
          </div>
        </div>
        {!studentId.trim() ? (
          <p className="empty-state">Enter a student ID to view applications.</p>
        ) : holds.length === 0 ? (
          <p className="empty-state">You have no applications yet.</p>
        ) : (
          <div className="applications">
            {holds.map((hold) => (
              <ApplicationCard
                key={hold.holdId}
                hold={hold}
                studentId={studentId}
                onConfirm={() =>
                  runAction(
                    () => confirmHold(hold.holdId, studentId.trim()),
                    'Admission confirmed successfully.'
                  )
                }
                onCancel={() =>
                  runAction(
                    () => cancelHold(hold.holdId, studentId.trim()),
                    'Application cancelled.'
                  )
                }
              />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}

function CreateClassForm({ onCreated }: { onCreated: () => void }) {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState('30');
  const [error, setError] = useState('');

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      await createClass(name, Number(totalSeats));
      setName('');
      setTotalSeats('30');
      setError('');
      onCreated();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to create class.');
    }
  };

  return (
    <form className="create-form panel" onSubmit={submit}>
      <div>
        <p className="eyebrow">Administration</p>
        <h2>Create a class</h2>
      </div>
      <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Class 6 - Morning Shift" />
      <input type="number" min="1" value={totalSeats} onChange={(event) => setTotalSeats(event.target.value)} />
      <button className="primary-button" type="submit">Create class</button>
      {error && <span className="form-error">{error}</span>}
    </form>
  );
}

function ClassCard({
  admissionClass,
  studentId,
  onHold
}: {
  admissionClass: AdmissionClass;
  studentId: string;
  onHold: () => void;
}) {
  return (
    <article className="class-card panel">
      <div className="card-title">
        <div>
          <span className="class-id">{admissionClass.id}</span>
          <h3>{admissionClass.name}</h3>
        </div>
        <strong className="available">{admissionClass.availableSeats}</strong>
      </div>
      <p className="available-label">available seats</p>
      <div className="stats">
        <span><b>{admissionClass.totalSeats}</b>Total</span>
        <span><b>{admissionClass.heldSeats}</b>Held</span>
        <span><b>{admissionClass.confirmedSeats}</b>Confirmed</span>
      </div>
      <button className="primary-button full-width" disabled={!studentId.trim() || admissionClass.availableSeats < 1} onClick={onHold}>
        {!studentId.trim() ? 'Enter student ID first' : 'Hold a seat'}
      </button>
    </article>
  );
}

function ApplicationCard({
  hold,
  studentId,
  onConfirm,
  onCancel
}: {
  hold: Hold;
  studentId: string;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const expiresAt = useMemo(() => new Date(hold.expiresAt), [hold.expiresAt]);
  const [remaining, setRemaining] = useState(() => Math.max(0, expiresAt.getTime() - Date.now()));

  useEffect(() => {
    const timer = window.setInterval(
      () => setRemaining(Math.max(0, expiresAt.getTime() - Date.now())),
      1000
    );
    return () => window.clearInterval(timer);
  }, [expiresAt]);

  const minutes = Math.floor(remaining / 60000);
  const seconds = Math.floor((remaining % 60000) / 1000).toString().padStart(2, '0');
  const active = hold.status === 'HELD' && remaining > 0;

  return (
    <article className="application panel">
      <div>
        <span className="class-id">{hold.classId}</span>
        <h3>Application {hold.holdId}</h3>
        <p className={`status ${hold.status.toLowerCase()}`}>{hold.status}</p>
      </div>
      <div className="application-meta">
        {active && <span className="countdown">{minutes}:{seconds} remaining</span>}
        {hold.status === 'HELD' && !active && <span className="expired-text">Hold expired</span>}
        {active && <button className="primary-button" onClick={onConfirm}>Pay admission fee</button>}
        {active && <button className="danger-button" onClick={onCancel}>Cancel</button>}
      </div>
    </article>
  );
}

export default App;

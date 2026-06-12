import type { ReactNode } from 'react';

type Tone = 'gray' | 'green' | 'red' | 'amber' | 'blue';

const TONES: Record<Tone, string> = {
  gray: 'bg-slate-100 text-slate-700 ring-slate-300',
  green: 'bg-green-50 text-green-700 ring-green-300',
  red: 'bg-red-50 text-red-700 ring-red-300',
  amber: 'bg-amber-50 text-amber-700 ring-amber-300',
  blue: 'bg-blue-50 text-blue-700 ring-blue-300',
};

export function Badge({ tone = 'gray', children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${TONES[tone]}`}
    >
      {children}
    </span>
  );
}

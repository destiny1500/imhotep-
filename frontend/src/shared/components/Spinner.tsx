const SIZES = {
  sm: 'h-4 w-4 border-2',
  md: 'h-6 w-6 border-2',
  lg: 'h-10 w-10 border-4',
} as const;

export function Spinner({ size = 'md', label }: { size?: keyof typeof SIZES; label?: string }) {
  return (
    <span role="status" className="inline-flex items-center gap-2">
      <span
        aria-hidden="true"
        className={`inline-block animate-spin rounded-full border-current border-t-transparent ${SIZES[size]}`}
      />
      <span className={label ? 'text-sm text-slate-500' : 'sr-only'}>
        {label ?? 'Chargement…'}
      </span>
    </span>
  );
}

export function PageSpinner({ label = 'Chargement…' }: { label?: string }) {
  return (
    <div className="flex justify-center py-16 text-brand-600">
      <Spinner size="lg" label={label} />
    </div>
  );
}

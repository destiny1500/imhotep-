import { useEffect, useRef } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { delegateProperty } from './api';
import { delegateSchema, type DelegateFormValues } from './schemas';
import { Input } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';

export function DelegateDialog({
  propertyId,
  open,
  onClose,
}: {
  propertyId: string;
  open: boolean;
  onClose: () => void;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DelegateFormValues>({
    resolver: zodResolver(delegateSchema),
    defaultValues: { agencyId: '', feePercent: 5 },
  });

  const delegate = useMutation({
    mutationFn: (values: DelegateFormValues) => delegateProperty(propertyId, values),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['properties'] });
      reset();
      onClose();
    },
  });

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      className="w-full max-w-md rounded-lg p-0 shadow-xl backdrop:bg-slate-900/40"
      aria-labelledby="delegate-dialog-title"
    >
      <div className="p-6">
        <h2 id="delegate-dialog-title" className="text-base font-semibold text-slate-900">
          Déléguer la gestion à une agence
        </h2>
        <p className="mt-1 text-sm text-slate-500">
          L'agence pourra gérer les baux, paiements et quittances de ce bien.
        </p>

        <form
          noValidate
          onSubmit={handleSubmit((values) => delegate.mutate(values))}
          className="mt-4 space-y-4"
        >
          <Input
            label="Identifiant de l'agence"
            placeholder="ex. 7f3c1e2a-…"
            error={errors.agencyId?.message}
            {...register('agencyId')}
          />
          <Input
            label="Honoraires (%)"
            type="number"
            step="0.1"
            min="0"
            max="100"
            error={errors.feePercent?.message}
            {...register('feePercent')}
          />

          {delegate.isError && <ErrorState message="La délégation a échoué." />}

          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={onClose}>
              Annuler
            </Button>
            <Button type="submit" isLoading={delegate.isPending}>
              Déléguer
            </Button>
          </div>
        </form>
      </div>
    </dialog>
  );
}

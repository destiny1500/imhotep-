import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createPayment } from './api';
import { paymentMethodLabels, paymentMethodValues, paymentSchema, type PaymentFormValues } from './schemas';
import { Input, Select } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';

export function RecordPaymentForm({ leaseId }: { leaseId: string }) {
  const queryClient = useQueryClient();
  const now = new Date();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PaymentFormValues>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      amount: 0,
      periodYear: now.getFullYear(),
      periodMonth: now.getMonth() + 1,
      paidAt: now.toISOString().slice(0, 10),
      method: 'BankTransfer',
    },
  });

  const record = useMutation({
    mutationFn: (values: PaymentFormValues) => createPayment({ leaseId, ...values }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['payments', leaseId] });
      reset();
    },
  });

  return (
    <form noValidate onSubmit={handleSubmit((values) => record.mutate(values))} className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Input label="Montant (€)" type="number" step="0.01" min="0" error={errors.amount?.message} {...register('amount')} />
        <Input label="Date de paiement" type="date" error={errors.paidAt?.message} {...register('paidAt')} />
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Input label="Année de la période" type="number" error={errors.periodYear?.message} {...register('periodYear')} />
        <Input label="Mois de la période" type="number" min="1" max="12" error={errors.periodMonth?.message} {...register('periodMonth')} />
        <Select label="Moyen de paiement" error={errors.method?.message} {...register('method')}>
          {paymentMethodValues.map((value) => (
            <option key={value} value={value}>
              {paymentMethodLabels[value]}
            </option>
          ))}
        </Select>
      </div>

      {record.isError && <ErrorState message="L'enregistrement du paiement a échoué." />}
      {record.isSuccess && (
        <p className="rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">
          Paiement enregistré.
        </p>
      )}

      <Button type="submit" isLoading={record.isPending}>
        Enregistrer le paiement
      </Button>
    </form>
  );
}

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getConversations, getMessages, sendMessage } from './api';
import { newMessageSchema, type NewMessageValues } from './schemas';
import { useAuthStore } from '@/features/auth/auth.store';
import { Button } from '@/shared/components/Button';
import { Textarea } from '@/shared/components/Input';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatDateTime } from '@/shared/lib/formatters';

export function ConversationPage() {
  const { id } = useParams<{ id: string }>();
  const userId = useAuthStore((s) => s.user?.id);
  const queryClient = useQueryClient();

  const conversations = useQuery({ queryKey: ['conversations'], queryFn: getConversations });
  const conversation = conversations.data?.find((c) => c.id === id);

  const { data: messages, isPending, isError } = useQuery({
    queryKey: ['messages', id],
    queryFn: () => getMessages(id!),
    enabled: Boolean(id),
    refetchInterval: 30_000,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<NewMessageValues>({
    resolver: zodResolver(newMessageSchema),
    defaultValues: { body: '' },
  });

  const send = useMutation({
    mutationFn: (values: NewMessageValues) => sendMessage(id!, values.body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['messages', id] });
      void queryClient.invalidateQueries({ queryKey: ['conversations'] });
      reset();
    },
  });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <Link to="/messages" className="text-sm text-brand-600 hover:text-brand-700">
          ← Toutes les conversations
        </Link>
        <h1 className="mt-1 text-xl font-bold text-slate-900">
          {conversation?.subject ?? 'Conversation'}
        </h1>
        {conversation && (
          <p className="text-sm text-slate-500">
            Avec {conversation.participants.map((p) => p.name).join(', ')}
          </p>
        )}
      </div>

      {isPending && <PageSpinner />}
      {isError && <ErrorState message="Impossible de charger les messages." />}

      {messages && messages.length === 0 && (
        <EmptyState title="Aucun message" description="Envoyez le premier message ci-dessous." />
      )}

      {messages && messages.length > 0 && (
        <ul className="space-y-3">
          {messages.map((message) => {
            const isMine = message.senderId === userId;
            return (
              <li key={message.id} className={`flex ${isMine ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-md rounded-lg px-4 py-2.5 text-sm shadow-sm ${
                    isMine
                      ? 'bg-brand-600 text-white'
                      : 'bg-white text-slate-900 ring-1 ring-slate-200'
                  }`}
                >
                  {!isMine && (
                    <p className="mb-0.5 text-xs font-semibold text-slate-500">
                      {message.senderName}
                    </p>
                  )}
                  <p className="whitespace-pre-wrap">{message.body}</p>
                  <p className={`mt-1 text-xs ${isMine ? 'text-brand-100' : 'text-slate-400'}`}>
                    {formatDateTime(message.sentAt)}
                  </p>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <form
        noValidate
        onSubmit={handleSubmit((values) => send.mutate(values))}
        className="space-y-3 rounded-lg bg-white p-4 shadow-sm ring-1 ring-slate-200"
      >
        <Textarea label="Votre message" error={errors.body?.message} {...register('body')} />
        {send.isError && <ErrorState message="L'envoi du message a échoué." />}
        <div className="flex justify-end">
          <Button type="submit" isLoading={send.isPending}>
            Envoyer
          </Button>
        </div>
      </form>
    </div>
  );
}

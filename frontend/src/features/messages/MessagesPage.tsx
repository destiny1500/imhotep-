import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { createConversation, getContacts, getConversations } from './api';
import { newConversationSchema, type NewConversationValues } from './schemas';
import { Card } from '@/shared/components/Card';
import { Button } from '@/shared/components/Button';
import { Input, Select, Textarea } from '@/shared/components/Input';
import { Badge } from '@/shared/components/Badge';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatDateTime } from '@/shared/lib/formatters';
import type { Role } from '@/shared/types/api';

const ROLE_LABELS: Record<Role, string> = {
  Tenant: 'Locataire',
  Owner: 'Propriétaire',
  Agency: 'Agence',
  Admin: 'Administrateur',
};

export function MessagesPage() {
  const [showNewForm, setShowNewForm] = useState(false);
  const { data: conversations, isPending, isError } = useQuery({
    queryKey: ['conversations'],
    queryFn: getConversations,
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold text-slate-900">Messages</h1>
        <Button onClick={() => setShowNewForm((v) => !v)}>
          {showNewForm ? 'Fermer' : 'Nouvelle conversation'}
        </Button>
      </div>

      {showNewForm && (
        <Card title="Nouvelle conversation">
          <NewConversationForm onCreated={() => setShowNewForm(false)} />
        </Card>
      )}

      {isPending && <PageSpinner />}
      {isError && <ErrorState message="Impossible de charger les conversations." />}

      {conversations && conversations.length === 0 && (
        <EmptyState
          title="Aucune conversation"
          description="Démarrez une conversation avec un propriétaire, un locataire ou une agence."
        />
      )}

      {conversations && conversations.length > 0 && (
        <ul className="divide-y divide-slate-100 overflow-hidden rounded-lg bg-white shadow-sm ring-1 ring-slate-200">
          {conversations.map((conversation) => (
            <li key={conversation.id}>
              <Link
                to={`/messages/${conversation.id}`}
                className="block px-4 py-3 hover:bg-slate-50"
              >
                <div className="flex items-center justify-between gap-3">
                  <p className="truncate font-medium text-slate-900">{conversation.subject}</p>
                  <p className="shrink-0 text-xs text-slate-400">
                    {formatDateTime(conversation.lastMessageAt)}
                  </p>
                </div>
                <div className="mt-1 flex flex-wrap gap-1.5">
                  {conversation.participants.map((participant) => (
                    <Badge key={participant.id} tone="gray">
                      {participant.name}
                    </Badge>
                  ))}
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function NewConversationForm({ onCreated }: { onCreated: () => void }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const {
    data: contacts,
    isPending: contactsPending,
    isError: contactsError,
  } = useQuery({
    queryKey: ['contacts'],
    queryFn: getContacts,
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<NewConversationValues>({
    resolver: zodResolver(newConversationSchema),
    defaultValues: { participantUserId: '', subject: '', body: '' },
  });

  const create = useMutation({
    mutationFn: createConversation,
    onSuccess: (conversation) => {
      void queryClient.invalidateQueries({ queryKey: ['conversations'] });
      onCreated();
      navigate(`/messages/${conversation.id}`);
    },
  });

  if (contactsPending) return <PageSpinner />;
  if (contactsError) return <ErrorState message="Impossible de charger les destinataires." />;
  if (!contacts || contacts.length === 0) {
    return (
      <EmptyState
        title="Aucun destinataire disponible"
        description="Vous pourrez écrire à vos contacts (propriétaire, locataires ou agence) dès qu'un bien vous liera à eux."
      />
    );
  }

  return (
    <form noValidate onSubmit={handleSubmit((values) => create.mutate(values))} className="space-y-4">
      <Select
        label="Destinataire"
        error={errors.participantUserId?.message}
        defaultValue=""
        {...register('participantUserId')}
      >
        <option value="" disabled>
          Sélectionnez un destinataire
        </option>
        {contacts.map((contact) => (
          <option key={contact.id} value={contact.id}>
            {contact.name} — {ROLE_LABELS[contact.role]}
          </option>
        ))}
      </Select>
      <Input label="Sujet" error={errors.subject?.message} {...register('subject')} />
      <Textarea label="Message" error={errors.body?.message} {...register('body')} />

      {create.isError && <ErrorState message="Impossible de créer la conversation." />}

      <Button type="submit" isLoading={create.isPending}>
        Envoyer
      </Button>
    </form>
  );
}

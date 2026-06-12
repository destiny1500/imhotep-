import { useRef, useState, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { uploadDocument } from './api';
import { documentTypeLabels, documentTypeValues } from './documentTypes';
import { Select } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';
import type { DocumentType } from '@/shared/types/api';

export function UploadDocumentForm({
  propertyId,
  leaseId,
}: {
  propertyId?: string;
  leaseId?: string;
}) {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [type, setType] = useState<DocumentType>('Other');
  const [fileError, setFileError] = useState<string | null>(null);

  const upload = useMutation({
    mutationFn: uploadDocument,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['documents'] });
      if (fileInputRef.current) fileInputRef.current.value = '';
    },
  });

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    const file = fileInputRef.current?.files?.[0];
    if (!file) {
      setFileError('Veuillez sélectionner un fichier');
      return;
    }
    setFileError(null);
    upload.mutate({ file, type, propertyId, leaseId });
  };

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="space-y-1">
          <label htmlFor="document-file" className="block text-sm font-medium text-slate-700">
            Fichier
          </label>
          <input
            ref={fileInputRef}
            id="document-file"
            type="file"
            aria-describedby={fileError ? 'document-file-error' : undefined}
            className="block w-full text-sm text-slate-600 file:mr-3 file:rounded-md file:border-0 file:bg-brand-50 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-brand-700 hover:file:bg-brand-100"
          />
          {fileError && (
            <p id="document-file-error" role="alert" className="text-sm text-red-600">
              {fileError}
            </p>
          )}
        </div>
        <Select
          label="Type de document"
          value={type}
          onChange={(e) => setType(e.target.value as DocumentType)}
        >
          {documentTypeValues.map((value) => (
            <option key={value} value={value}>
              {documentTypeLabels[value]}
            </option>
          ))}
        </Select>
      </div>

      {upload.isError && <ErrorState message="Le dépôt du document a échoué." />}
      {upload.isSuccess && (
        <p className="rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">Document déposé.</p>
      )}

      <Button type="submit" isLoading={upload.isPending}>
        Déposer le document
      </Button>
    </form>
  );
}

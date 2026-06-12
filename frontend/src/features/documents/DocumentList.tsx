import { useMutation, useQuery } from '@tanstack/react-query';
import { downloadDocument, getDocuments, type DocumentFilter } from './api';
import { documentTypeLabels } from './documentTypes';
import { Badge } from '@/shared/components/Badge';
import { Button } from '@/shared/components/Button';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { Spinner } from '@/shared/components/Spinner';
import { downloadBlob } from '@/shared/lib/download';
import { formatDate, formatFileSize } from '@/shared/lib/formatters';
import type { DocumentItem } from '@/shared/types/api';

export function DocumentList({ filter }: { filter: DocumentFilter }) {
  const { data: documents, isPending, isError } = useQuery({
    queryKey: ['documents', filter],
    queryFn: () => getDocuments(filter),
  });

  const download = useMutation({
    mutationFn: async (doc: DocumentItem) => {
      const blob = await downloadDocument(doc.id);
      downloadBlob(blob, doc.fileName);
    },
  });

  if (isPending) return <Spinner label="Chargement des documents…" />;
  if (isError) return <ErrorState message="Impossible de charger les documents." />;
  if (documents.length === 0) {
    return <EmptyState title="Aucun document" description="Aucun document n'a encore été déposé." />;
  }

  return (
    <table className="min-w-full divide-y divide-slate-200 text-sm">
      <thead className="text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
        <tr>
          <th scope="col" className="py-2 pr-4">Fichier</th>
          <th scope="col" className="py-2 pr-4">Type</th>
          <th scope="col" className="py-2 pr-4">Taille</th>
          <th scope="col" className="py-2 pr-4">Ajouté le</th>
          <th scope="col" className="py-2">
            <span className="sr-only">Actions</span>
          </th>
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {documents.map((doc) => (
          <tr key={doc.id}>
            <td className="py-2.5 pr-4 font-medium text-slate-900">{doc.fileName}</td>
            <td className="py-2.5 pr-4">
              <Badge tone="blue">{documentTypeLabels[doc.type]}</Badge>
            </td>
            <td className="py-2.5 pr-4 text-slate-600">{formatFileSize(doc.sizeBytes)}</td>
            <td className="py-2.5 pr-4 text-slate-600">{formatDate(doc.createdAt)}</td>
            <td className="py-2.5 text-right">
              <Button
                variant="secondary"
                onClick={() => download.mutate(doc)}
                isLoading={download.isPending && download.variables?.id === doc.id}
              >
                Télécharger
              </Button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

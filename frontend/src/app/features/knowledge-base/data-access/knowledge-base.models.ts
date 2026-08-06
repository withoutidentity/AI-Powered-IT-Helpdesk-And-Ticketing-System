export type KnowledgeDocumentStatus = 'Processing' | 'Ready' | 'Failed';

export interface PaginatedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface KnowledgeDocumentSummary {
  id: string;
  title: string;
  sourceFile: string;
  sourceType: string;
  contentHash: string;
  status: KnowledgeDocumentStatus;
  failureReason: string | null;
  chunkCount: number;
  uploadedAt: string;
  updatedAt: string;
}

export interface DocumentChunk {
  id: string;
  documentId: string;
  chunkIndex: number;
  content: string;
  contentHash: string;
  tokenCount: number | null;
  embeddingModel: string | null;
  embeddingDimensions: number | null;
  createdAt: string;
}

export interface KnowledgeDocumentDetail extends Omit<KnowledgeDocumentSummary, 'chunkCount'> {
  chunks: DocumentChunk[];
}

export interface CreateKnowledgeDocumentRequest {
  title: string;
  sourceFile: string;
  sourceType: string;
  content: string;
}
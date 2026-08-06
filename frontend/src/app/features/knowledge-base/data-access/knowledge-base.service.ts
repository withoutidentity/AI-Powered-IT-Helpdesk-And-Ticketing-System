import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateKnowledgeDocumentRequest, KnowledgeDocumentDetail, KnowledgeDocumentSummary, PaginatedList } from './knowledge-base.models';

@Injectable({ providedIn: 'root' })
export class KnowledgeBaseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/kb/documents`;

  listDocuments(page = 1, pageSize = 20): Observable<PaginatedList<KnowledgeDocumentSummary>> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));

    return this.http.get<PaginatedList<KnowledgeDocumentSummary>>(this.baseUrl, { params });
  }

  getDocument(documentId: string): Observable<KnowledgeDocumentDetail> {
    return this.http.get<KnowledgeDocumentDetail>(`${this.baseUrl}/${documentId}`);
  }

  createDocument(request: CreateKnowledgeDocumentRequest): Observable<KnowledgeDocumentDetail> {
    return this.http.post<KnowledgeDocumentDetail>(this.baseUrl, request);
  }
}
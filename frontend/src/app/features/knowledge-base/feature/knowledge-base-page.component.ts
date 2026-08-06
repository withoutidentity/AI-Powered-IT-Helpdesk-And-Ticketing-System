import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { KnowledgeBaseService } from '../data-access/knowledge-base.service';
import { KnowledgeDocumentDetail, KnowledgeDocumentSummary } from '../data-access/knowledge-base.models';

@Component({
  selector: 'app-knowledge-base-page',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './knowledge-base-page.component.html',
  styleUrl: './knowledge-base-page.component.scss',
})
export class KnowledgeBasePageComponent implements OnInit {
  private readonly knowledgeBaseService = inject(KnowledgeBaseService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly documents = signal<KnowledgeDocumentSummary[]>([]);
  readonly selectedDocument = signal<KnowledgeDocumentDetail | null>(null);
  readonly selectedDocumentId = signal<string | null>(null);
  readonly isLoadingDocuments = signal(false);
  readonly isLoadingDetail = signal(false);
  readonly isCreating = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly createMessage = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly isITAdmin = computed(() => this.auth.currentUser()?.role === 'ITAdmin');
  readonly canViewKnowledgeBase = computed(() => {
    const role = this.auth.currentUser()?.role;
    return role === 'ITAdmin' || role === 'ITAgent';
  });
  readonly hasDocuments = computed(() => this.documents().length > 0);

  readonly createForm = this.formBuilder.nonNullable.group({
    title: [''],
    sourceFile: [''],
    sourceType: ['text/markdown'],
    content: [''],
  });

  ngOnInit(): void {
    if (this.canViewKnowledgeBase()) {
      this.loadDocuments();
    }
  }

  selectDocument(document: KnowledgeDocumentSummary): void {
    this.selectedDocumentId.set(document.id);
    this.selectedDocument.set(null);
    this.errorMessage.set(null);
    this.createMessage.set(null);
    this.isLoadingDetail.set(true);

    this.knowledgeBaseService.getDocument(document.id).pipe(
      finalize(() => this.isLoadingDetail.set(false)),
    ).subscribe({
      next: (detail) => this.selectedDocument.set(detail),
      error: (error) => this.setError(error, 'Could not load document detail.'),
    });
  }

  createDocument(): void {
    if (!this.isITAdmin() || this.isCreating()) {
      return;
    }

    const value = this.createForm.getRawValue();
    const title = value.title.trim();
    const sourceFile = value.sourceFile.trim();
    const sourceType = value.sourceType.trim();
    const content = value.content.trim();

    if (!title || !sourceFile || !sourceType || !content) {
      this.errorMessage.set('Title, source file, source type, and content are required.');
      return;
    }

    this.errorMessage.set(null);
    this.createMessage.set(null);
    this.isCreating.set(true);

    this.knowledgeBaseService.createDocument({ title, sourceFile, sourceType, content }).pipe(
      finalize(() => this.isCreating.set(false)),
    ).subscribe({
      next: (detail) => {
        this.createForm.reset({ title: '', sourceFile: '', sourceType: 'text/markdown', content: '' });
        this.createMessage.set('Document added.');
        this.loadDocuments(detail.id);
      },
      error: (error) => this.setError(error, 'Could not create document.'),
    });
  }

  isSelected(document: KnowledgeDocumentSummary): boolean {
    return this.selectedDocumentId() === document.id;
  }

  statusLabel(status: string): string {
    return status;
  }

  private loadDocuments(selectDocumentId?: string): void {
    this.errorMessage.set(null);
    this.isLoadingDocuments.set(true);

    this.knowledgeBaseService.listDocuments(this.page(), this.pageSize).pipe(
      finalize(() => this.isLoadingDocuments.set(false)),
    ).subscribe({
      next: (response) => {
        this.documents.set(response.items);
        this.totalCount.set(response.totalCount);

        const target = selectDocumentId
          ? response.items.find((document) => document.id === selectDocumentId)
          : response.items.find((document) => document.id === this.selectedDocumentId()) ?? response.items[0];

        if (target) {
          this.selectDocument(target);
          return;
        }

        this.selectedDocumentId.set(null);
        this.selectedDocument.set(null);
      },
      error: (error) => this.setError(error, 'Could not load knowledge-base documents.'),
    });
  }

  private setError(error: { status?: number } | null | undefined, fallback: string): void {
    if (error?.status === 403) {
      this.errorMessage.set('You do not have permission to use the knowledge base admin area.');
      return;
    }

    if (error?.status === 404) {
      this.errorMessage.set('Document was not found.');
      return;
    }

    if (error?.status === 400) {
      this.errorMessage.set('The submitted document data is invalid.');
      return;
    }

    this.errorMessage.set(fallback);
  }
}
export interface Note {
    id: string;
    moduleId: string;
    sourceMaterialId?: string | null;
    title: string;
    content: string | null;
    userContent?: string | null;
    aiContent?: string | null;
    includeInFinalExam?: boolean;
    createdAt: string;
    embeddedImages?: NoteEmbeddedImage[] | null;
}

export interface NoteEmbeddedImage {
    id: string;
    blobPath: string;
    fileName: string;
    pageNumber: number;
    downloadUrl?: string | null;
}

export interface CreateNoteRequest {
    moduleId: string;
    title: string;
    content?: string;
    userContent?: string;
    aiContent?: string;
}

export interface UpdateNoteRequest {
    title: string;
    content?: string;
    userContent?: string;
    aiContent?: string;
}

export interface NoteExamInclusionRequest {
    includeInFinalExam: boolean;
}

export interface NoteSourceDocumentDto {
    documentId: string;
    fileName: string;
    status: string;
    createdAt: string;
    fileSizeBytes?: number | null;
    downloadUrl?: string | null;
}

export interface NoteSourceExtractionDto {
    extractedContentId: string;
    documentId: string;
    fileName: string;
    extractedAt: string;
    status: string;
    isSaved: boolean;
    downloadUrl?: string | null;
}

export interface NoteSourcesDto {
    noteId: string;
    moduleId: string;
    uploadedDocuments: NoteSourceDocumentDto[];
    extractedDocuments: NoteSourceExtractionDto[];
}

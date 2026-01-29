export interface Note {
    id: string;
    moduleId: string;
    sourceMaterialId?: string | null;
    title: string;
    content: string | null;
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
}

export interface UpdateNoteRequest {
    title: string;
    content?: string;
}

export interface Note {
    id: string;
    moduleId: string;
    title: string;
    content: string | null;
    createdAt: string;
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

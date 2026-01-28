export interface ModuleDto {
    id: string;
    title: string;
    description?: string;
    createdAt: string;
    documentsCount: number;
    notesCount: number;
    quizzesCount: number;
}

export interface CreateModuleDto {
    title: string;
    description?: string;
}

export interface UpdateModuleDto {
    title: string;
    description?: string;
}

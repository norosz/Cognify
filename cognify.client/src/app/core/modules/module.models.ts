export interface ModuleDto {
    id: string;
    title: string;
    description?: string;
    createdAt: string;
}

export interface CreateModuleDto {
    title: string;
    description?: string;
}

export interface UpdateModuleDto {
    title: string;
    description?: string;
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ModuleDto, CreateModuleDto, UpdateModuleDto, ModuleStatsDto } from './module.models';

@Injectable({
    providedIn: 'root'
})
export class ModuleService {
    private readonly apiUrl = '/api/modules';

    constructor(private http: HttpClient) { }

    getModules(): Observable<ModuleDto[]> {
        return this.http.get<ModuleDto[]>(this.apiUrl);
    }

    getModule(id: string): Observable<ModuleDto> {
        return this.http.get<ModuleDto>(`${this.apiUrl}/${id}`);
    }

    getModuleStats(id: string): Observable<ModuleStatsDto> {
        return this.http.get<ModuleStatsDto>(`${this.apiUrl}/${id}/stats`);
    }

    createModule(dto: CreateModuleDto): Observable<ModuleDto> {
        return this.http.post<ModuleDto>(this.apiUrl, dto);
    }

    updateModule(id: string, dto: UpdateModuleDto): Observable<ModuleDto> {
        return this.http.put<ModuleDto>(`${this.apiUrl}/${id}`, dto);
    }

    deleteModule(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}

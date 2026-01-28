export interface AuthResponse {
    token: string;
    email: string;
    userId: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
    username?: string;
}

export interface UserProfile {
    id: string;
    email: string;
    username?: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface UpdateProfileRequest {
    email: string;
    username?: string;
}

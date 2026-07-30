export type UserRole = 'Employee' | 'ITAgent' | 'ITAdmin';

export interface AuthUser {
  id: string;
  username: string;
  role: UserRole;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  role: UserRole;
  department: string;
}

export interface RegisterResponse {
  id: string;
  username: string;
  email: string;
  role: UserRole;
  department: string;
  createdAt: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: AuthUser;
}

export interface RefreshRequest {
  refreshToken: string;
}
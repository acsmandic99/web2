import type { ResultDto } from '../types/shared/ResultDto';
import type { UserLoginDto } from '../types/auth/UserLoginDto';
import type { UserRegisterDto } from '../types/auth/UserRegisterDto';
import apiClient from './apiClient';

const AUTH_PREFIX = 'api/auth';

export const authService = {
  async login(credentials: UserLoginDto): Promise<ResultDto<string>> {
    if (!credentials.username.trim() || !credentials.password.trim()) {
      throw new Error('Username and password are required fields.');
    }
    const response = await apiClient.post<ResultDto<string>>(`/${AUTH_PREFIX}/login`, credentials);
    return response.data;
  },

  async register(userData: UserRegisterDto): Promise<ResultDto<string>> {
    if (!userData.username.trim() || !userData.email.trim() || !userData.password.trim()) {
      throw new Error('All registration fields are required.');
    }
    const response = await apiClient.post<ResultDto<string>>(`/${AUTH_PREFIX}/register`, userData);
    return response.data;
  }
};
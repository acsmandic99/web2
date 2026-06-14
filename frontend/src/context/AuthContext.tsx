import React, { createContext, useContext, useState, useEffect } from 'react';
import type { UserDto } from '../types/auth/UserDto';
import type { UserLoginDto } from '../types/auth/UserLoginDto';
import type { UserRegisterDto } from '../types/auth/UserRegisterDto';
import { authService } from '../services/authService';

interface AuthContextType {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: UserLoginDto) => Promise<void>;
  register: (userData: UserRegisterDto) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const parseJwt = (token: string): UserDto | null => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    const payload = JSON.parse(jsonPayload);
    return {
      id: payload.nameid || '',
      username: payload.unique_name || '',
      email: payload.email || ''
    };
  } catch (e) {
    return null;
  }
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserDto | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    try {
      const storedToken = localStorage.getItem('token');
      const storedUser = localStorage.getItem('user');

      if (storedToken && storedUser && storedUser !== 'undefined') {
        setToken(storedToken);
        setUser(JSON.parse(storedUser));
      }
    } catch (e) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const login = async (credentials: UserLoginDto) => {
    const result = await authService.login(credentials);
    if (result.isSuccess && result.data) {
      const jwtToken = result.data;
      const decodedUser = parseJwt(jwtToken);

      if (!decodedUser) {
        throw new Error('Invalid token payload.');
      }

      setToken(jwtToken);
      setUser(decodedUser);
      localStorage.setItem('token', jwtToken);
      localStorage.setItem('user', JSON.stringify(decodedUser));
    } else {
      throw new Error(result.message || 'Login failed.');
    }
  };

  const register = async (userData: UserRegisterDto) => {
    const result = await authService.register(userData);
    if (result.isSuccess && result.data) {
      const jwtToken = result.data;
      const decodedUser = parseJwt(jwtToken);

      if (!decodedUser) {
        throw new Error('Invalid token payload.');
      }

      setToken(jwtToken);
      setUser(decodedUser);
      localStorage.setItem('token', jwtToken);
      localStorage.setItem('user', JSON.stringify(decodedUser));
    } else {
      throw new Error(result.message || 'Registration failed.');
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  };

  const isAuthenticated = !!token;

  return (
    <AuthContext.Provider value={{ user, token, isAuthenticated, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
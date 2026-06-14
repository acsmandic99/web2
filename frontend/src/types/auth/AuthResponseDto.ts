import type { UserDto } from "./UserDto";

export interface AuthResponseDto {
  token: string;
  message: string;
  user: UserDto;
}
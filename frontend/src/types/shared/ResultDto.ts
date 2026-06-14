export interface ResultDto<T> {
  isSuccess: boolean;
  message: string;
  data: T;
}
// Auth types
export interface LoginInfo {
  email: string
  password: string
}

export interface RegisterInfo {
  email: string
  password: string
  firstname: string
  lastname: string
}

export interface JWTResponse {
  jwt: string
  refreshToken: string
}

export interface TokenRefreshInfo {
  jwt: string
  refreshToken: string
}

export interface LogoutInfo {
  refreshToken: string
}

// Vehicle types
export interface VehicleDto {
  id: string
  make: string
  model: string
  year: number
  licensePlate: string
  vin?: string
  mileage?: number
  color?: string
  ownerId: string
}

export interface VehicleCreateDto {
  make: string
  model: string
  year: number
  licensePlate: string
  vin?: string
  mileage?: number
  color?: string
}

// Service Order types
export interface ServiceOrderDto {
  id: string
  description?: string
  status: string
  orderDate: string
  completedDate?: string
  vehicleId: string
  vehicleDisplay?: string
  workshopId: string
  workshopName?: string
  mechanicId?: string
  mechanicName?: string
  totalAmount: number
}

export interface ServiceOrderCreateDto {
  vehicleId: string
  workshopId: string
  description?: string
}

export interface StatusHistoryEntry {
  id: string
  status: string
  notes?: string
  changedAt: string
}

// Payment types
export interface PaymentDto {
  id: string
  amount: number
  status: string
  paidAt?: string
  paymentMethod?: string
  notes?: string
  serviceOrderId: string
  createdAt: string
}

// Workshop types
export interface WorkshopDto {
  id: string
  name: string
  address: string
  phone?: string
  email?: string
}

// Mechanic types
export interface MechanicDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  phone?: string
  email?: string
  specialization?: string
}

// Error response
export interface RestApiErrorResponse {
  status: number
  error: string
}

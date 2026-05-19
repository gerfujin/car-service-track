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

// Service types
export interface ServiceDto {
  id: string
  name: string
  description: string
  basePrice: number
  estimatedTimeMinutes: number
}

export interface SparePartDto {
  id: string
  name: string
  partNumber?: string
  manufacturer?: string
  price: number
  country?: string
  stockQuantity: number
}

export interface SparePartCreateDto {
  name: string
  partNumber?: string
  manufacturer?: string
  price: number
  country?: string
  stockQuantity: number
}

export interface SparePartUpdateDto {
  name: string
  partNumber?: string
  manufacturer?: string
  price: number
  country?: string
  stockQuantity: number
}

export interface ServiceOrderPartDto {
  id: string
  serviceOrderId: string
  sparePartId: string
  sparePartName?: string
  sparePartPartNumber?: string
  quantity: number
  price: number
  lineTotal: number
}

export interface ServiceOrderPartCreateDto {
  quantity: number
  price: number
  sparePartId: string
  serviceOrderId: string
}

export interface ServiceOrderPartUpdateDto {
  quantity: number
  price: number
  sparePartId: string
  serviceOrderId: string
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
  finalPrice?: number
  services?: ServiceDto[]
}

export interface ServiceOrderCreateDto {
  vehicleId: string
  workshopId: string
  description?: string
  serviceIds?: string[]
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
  statusName: string
  paidAt?: string
  paymentMethod?: string
  notes?: string
  serviceOrderId: string
  ownerId?: string
  createdAt: string
  vehicleInfo?: string
  workshopName?: string
}

export interface PaymentCreateDto {
  serviceOrderId: string
  amount: number
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

// Repair Photo types
export interface RepairPhotoDto {
  id: string
  description?: string
  photoUrl?: string
  uploadedAt: string
  serviceOrderId: string
}

// Error response
export interface RestApiErrorResponse {
  status: number
  error: string
}

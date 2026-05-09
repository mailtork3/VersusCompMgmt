export interface Company {
  id: number;
  name: string;
  websiteUrl: string;
  createdAt: Date;
}

export interface CompanyCreatedResponse extends Company {
  relevanceScore: number;
}

export interface CreateCompanyRequest {
  name: string;
  websiteUrl: string;
}

export interface ApiError {
  error: string;
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Company, CompanyCreatedResponse, CreateCompanyRequest } from '../models/company';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private apiUrl = '/api/companies';

  constructor(private http: HttpClient) {}

  createCompany(request: CreateCompanyRequest): Observable<CompanyCreatedResponse> {
    return this.http.post<CompanyCreatedResponse>(this.apiUrl, request).pipe(
      catchError(this.handleError)
    );
  }

  getAllCompanies(): Observable<Company[]> {
    return this.http.get<Company[]>(this.apiUrl).pipe(
      catchError(this.handleError)
    );
  }

  getCompanyById(id: number): Observable<Company> {
    return this.http.get<Company>(`${this.apiUrl}/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  searchByName(name: string): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.apiUrl}/search`, {
      params: { name }
    }).pipe(
      catchError(this.handleError)
    );
  }

  searchByDomain(domain: string): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.apiUrl}/search`, {
      params: { domain }
    }).pipe(
      catchError(this.handleError)
    );
  }

  searchByRelevance(search: string): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.apiUrl}/relevance`, {
      params: { search }
    }).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: any): Observable<never> {
    let errorMessage: string;

    if (error.error instanceof ErrorEvent) {
      errorMessage = 'Network error: ' + error.error.message;
    } else if (error.status === 0) {
      errorMessage = 'Unable to reach the server. Please check your connection.';
    } else if (error.error?.error) {
      errorMessage = error.error.error;
    } else if (error.error?.title) {
      errorMessage = error.error.title;
    } else if (error.status === 400) {
      errorMessage = 'Invalid request. Please check your input.';
    } else if (error.status === 404) {
      errorMessage = 'The requested resource was not found.';
    } else if (error.status === 500) {
      errorMessage = 'Server error. Please try again later.';
    } else {
      errorMessage = `Unexpected error (${error.status ?? 'unknown'}).`;
    }

    return throwError(() => new Error(errorMessage));
  }
}

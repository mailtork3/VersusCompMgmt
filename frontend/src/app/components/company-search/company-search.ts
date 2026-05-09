import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompanyService } from '../../services/company.service';
import { Company } from '../../models/company';

@Component({
  selector: 'app-company-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './company-search.html',
  styleUrl: './company-search.css',
})
export class CompanySearchComponent {
  searchName = '';
  searchDomain = '';
  searchRelevance = '';
  results: Company[] = [];
  isSearching = false;
  errorMessage: string | null = null;
  searchPerformed = false;
  activeTab: 'name' | 'domain' | 'relevance' = 'name';

  constructor(private companyService: CompanyService, private cdr: ChangeDetectorRef) {}

  searchByName() {
    if (!this.searchName.trim()) {
      this.errorMessage = 'Please enter a company name to search.';
      return;
    }
    this.performSearch(() => this.companyService.searchByName(this.searchName));
  }

  searchByDomain() {
    if (!this.searchDomain.trim()) {
      this.errorMessage = 'Please enter a domain to search.';
      return;
    }
    this.performSearch(() => this.companyService.searchByDomain(this.searchDomain));
  }

  searchByRelevance() {
    if (!this.searchRelevance.trim()) {
      this.errorMessage = 'Please enter a search term for relevance search.';
      return;
    }
    this.performSearch(() => this.companyService.searchByRelevance(this.searchRelevance));
  }

  private performSearch(searchFn: () => any) {
    this.isSearching = true;
    this.errorMessage = null;
    this.searchPerformed = true;

    searchFn().subscribe({
      next: (data: Company[]) => {
        this.results = data.map(c => ({
          ...c,
          createdAt: new Date(c.createdAt)
        }));
        this.isSearching = false;
        this.cdr.markForCheck();
      },
      error: (error: Error) => {
        this.errorMessage = error.message;
        this.results = [];
        this.isSearching = false;
        this.cdr.markForCheck();
      }
    });
  }

  clearSearch() {
    this.searchName = '';
    this.searchDomain = '';
    this.searchRelevance = '';
    this.results = [];
    this.errorMessage = null;
    this.searchPerformed = false;
  }
}

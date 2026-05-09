import { Component, OnInit, OnChanges, Input, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompanyService } from '../../services/company.service';
import { Company } from '../../models/company';

type SearchType = 'name' | 'domain' | 'relevance';

@Component({
  selector: 'app-company-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './company-list.html',
  styleUrl: './company-list.css',
})
export class CompanyListComponent implements OnInit, OnChanges {
  @Input() refreshTrigger = 0;

  companies: Company[] = [];
  allCompanies: Company[] = [];

  searchTerm = '';
  searchType: SearchType = 'name';
  searchActive = false;

  isLoading = false;
  isSearching = false;
  errorMessage: string | null = null;

  constructor(private companyService: CompanyService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadCompanies();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadCompanies();
    }
  }

  loadCompanies() {
    this.isLoading = true;
    this.errorMessage = null;
    this.searchActive = false;
    this.searchTerm = '';

    this.companyService.getAllCompanies().subscribe({
      next: (data: Company[]) => {
        this.allCompanies = data.map(c => ({ ...c, createdAt: new Date(c.createdAt) }));
        this.companies = [...this.allCompanies];
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: Error) => {
        this.errorMessage = error.message;
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  search() {
    if (!this.searchTerm.trim()) {
      this.errorMessage = 'Please enter a search term.';
      this.cdr.markForCheck();
      return;
    }

    this.isSearching = true;
    this.errorMessage = null;

    const obs = this.searchType === 'name'
      ? this.companyService.searchByName(this.searchTerm)
      : this.searchType === 'domain'
        ? this.companyService.searchByDomain(this.searchTerm)
        : this.companyService.searchByRelevance(this.searchTerm);

    obs.subscribe({
      next: (data: Company[]) => {
        this.companies = data.map(c => ({ ...c, createdAt: new Date(c.createdAt) }));
        this.searchActive = true;
        this.isSearching = false;
        this.cdr.markForCheck();
      },
      error: (error: Error) => {
        this.errorMessage = error.message;
        this.isSearching = false;
        this.cdr.markForCheck();
      }
    });
  }

  clearSearch() {
    this.searchTerm = '';
    this.searchActive = false;
    this.errorMessage = null;
    this.companies = [...this.allCompanies];
    this.cdr.markForCheck();
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.search();
    }
  }
}

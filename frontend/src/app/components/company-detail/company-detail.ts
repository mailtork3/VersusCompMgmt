import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { CompanyService } from '../../services/company.service';
import { Company } from '../../models/company';

@Component({
  selector: 'app-company-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './company-detail.html',
  styleUrl: './company-detail.css',
})
export class CompanyDetailComponent implements OnInit {
  company: Company | null = null;
  isLoading = false;
  errorMessage: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private companyService: CompanyService
  ) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.loadCompany(id);
    } else {
      this.errorMessage = 'Invalid company ID';
    }
  }

  loadCompany(id: number) {
    this.isLoading = true;
    this.errorMessage = null;

    this.companyService.getCompanyById(id).subscribe({
      next: (data: Company) => {
        this.company = {
          ...data,
          createdAt: new Date(data.createdAt)
        };
        this.isLoading = false;
      },
      error: (error: Error) => {
        this.errorMessage = error.message;
        this.isLoading = false;
      }
    });
  }

  goBack() {
    this.router.navigate(['/']);
  }
}

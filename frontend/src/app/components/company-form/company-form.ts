import { Component, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CompanyService } from '../../services/company.service';
import { Company, CompanyCreatedResponse } from '../../models/company';

@Component({
  selector: 'app-company-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './company-form.html',
  styleUrl: './company-form.css',
})
export class CompanyFormComponent {
  @Output() companyCreated = new EventEmitter<Company>();

  form: FormGroup;
  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  constructor(
    private fb: FormBuilder,
    private companyService: CompanyService,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      websiteUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/)]]
    });
  }

  get name() {
    return this.form.get('name');
  }

  get websiteUrl() {
    return this.form.get('websiteUrl');
  }

  onSubmit() {
    if (this.form.invalid) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;
    this.successMessage = null;

    const { name, websiteUrl } = this.form.value;
    this.companyService.createCompany({ name, websiteUrl }).subscribe({
      next: (company: CompanyCreatedResponse) => {
        this.isSubmitting = false;
        this.successMessage = `Company created successfully! Relevance score: ${company.relevanceScore}%`;
        this.form.reset();
        this.companyCreated.emit(company);
        this.cdr.markForCheck();
        setTimeout(() => { this.successMessage = null; this.cdr.markForCheck(); }, 3000);
      },
      error: (error: Error) => {
        this.isSubmitting = false;
        this.errorMessage = error.message;
        this.cdr.markForCheck();
      }
    });
  }
}

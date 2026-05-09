import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CompanyFormComponent } from './components/company-form/company-form';
import { CompanyListComponent } from './components/company-list/company-list';
import { Company } from './models/company';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    CompanyFormComponent,
    CompanyListComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Company Management');
  refreshCounter = signal(0);

  onCompanyCreated(_company: Company) {
    this.refreshCounter.update(value => value + 1);
  }
}

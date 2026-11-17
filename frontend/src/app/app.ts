import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './shared/theme/theme.service';
import { ValidationSummaryComponent } from './shared/components/validation-summary/validation-summary.component';

@Component({
  imports: [RouterOutlet, ValidationSummaryComponent],
  selector: 'app-root',
  template: '<router-outlet /><app-validation-summary />',
})
export class App {
  readonly theme = inject(ThemeService);
}

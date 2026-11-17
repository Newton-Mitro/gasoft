import { AfterViewInit, Component, OnDestroy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './shared/theme/theme.service';
import { ValidationSummaryComponent } from './shared/components/validation-summary/validation-summary.component';

@Component({
  imports: [RouterOutlet, ValidationSummaryComponent],
  selector: 'app-root',
  template: '<router-outlet /><app-validation-summary />',
})
export class App implements AfterViewInit, OnDestroy {
  readonly theme = inject(ThemeService);
  private readonly tableObserver = new MutationObserver(() => this.updateTableLabels());

  ngAfterViewInit(): void {
    this.updateTableLabels();
    this.tableObserver.observe(document.body, { childList: true, subtree: true });
  }

  ngOnDestroy(): void {
    this.tableObserver.disconnect();
  }

  private updateTableLabels(): void {
    for (const table of document.querySelectorAll<HTMLTableElement>('.content-area table')) {
      const headerRow = table.tHead?.rows.item(table.tHead.rows.length - 1);
      if (!headerRow) continue;

      const labels = Array.from(headerRow.cells, cell => cell.textContent?.trim() ?? '');
      for (const row of table.tBodies) {
        for (const bodyRow of row.rows) {
          Array.from(bodyRow.cells).forEach((cell, index) => {
            const label = labels[index];
            if (label && cell.getAttribute('data-label') !== label) {
              cell.setAttribute('data-label', label);
            } else if (!label && cell.hasAttribute('data-label')) {
              cell.removeAttribute('data-label');
            }
          });
        }
      }
    }
  }
}

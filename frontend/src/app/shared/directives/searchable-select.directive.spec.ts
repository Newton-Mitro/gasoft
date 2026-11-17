import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { SearchableSelectDirective } from './searchable-select.directive';

@Component({
  imports: [FormsModule, SearchableSelectDirective],
  template: `
    @for (field of fields; track field.key) {
      @if (field.type === 'select') {
        <label>
          <span>{{ field.label }}</span>
          <select appSearchableSelect name="choice" [(ngModel)]="value">
            <option [ngValue]="null" disabled>Select an option</option>
            <option [ngValue]="1">First item</option>
            <option [ngValue]="2">Second item</option>
          </select>
        </label>
      }
    }
  `,
})
class SearchableSelectHost {
  fields = [{ key: 'choice', type: 'select', label: 'Choice' }];
  value: number | null = 2;
}

describe('SearchableSelectDirective', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('filters options and updates the Angular model with the selected value', async () => {
    const fixture = TestBed.createComponent(SearchableSelectHost);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    expect(select.parentElement?.classList.contains('searchable-select')).toBe(true);
    expect(getComputedStyle(select).display).toBe('none');

    const trigger = fixture.nativeElement.querySelector('.searchable-select-trigger') as HTMLButtonElement;
    expect(trigger.textContent).toContain('Second item');

    trigger.click();
    const search = document.body.querySelector('.searchable-select-search') as HTMLInputElement;
    search.value = 'first';
    search.dispatchEvent(new Event('input', { bubbles: true }));

    const options = document.body.querySelectorAll('.searchable-select-option');
    expect(options).toHaveLength(1);
    expect(options[0].textContent).toBe('First item');

    (options[0] as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.value).toBe(1);
    expect(trigger.textContent).toContain('First item');
    expect(document.body.querySelector('.searchable-select-dropdown')).toBeNull();
    fixture.destroy();
  });
});

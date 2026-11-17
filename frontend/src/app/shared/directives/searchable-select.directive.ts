import {
  Directive,
  DoCheck,
  DestroyRef,
  ElementRef,
  HostListener,
  Renderer2,
  inject,
} from '@angular/core';

let nextSelectId = 0;

@Directive({
  selector: 'select[appSearchableSelect]',
  standalone: true,
})
export class SearchableSelectDirective implements DoCheck {
  private readonly select = inject(ElementRef<HTMLSelectElement>).nativeElement;
  private readonly renderer = inject(Renderer2);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = `searchable-select-${nextSelectId++}`;
  private readonly wrapper = this.renderer.createElement('div') as HTMLDivElement;
  private readonly trigger = this.renderer.createElement('button') as HTMLButtonElement;
  private readonly dropdown = this.renderer.createElement('div') as HTMLDivElement;
  private readonly search = this.renderer.createElement('input') as HTMLInputElement;
  private readonly optionsList = this.renderer.createElement('div') as HTMLDivElement;
  private readonly valueText = this.renderer.createElement('span') as HTMLSpanElement;
  private readonly observer = new MutationObserver(() => this.refresh());
  private readonly unlisten: Array<() => void> = [];
  private isOpen = false;
  private initialized = false;
  private renderedValue = '';
  private renderedHeight = '';

  constructor() {
    if (this.select.multiple) return;
    this.initialize();
  }

  private initialize(): boolean {
    const parent = this.select.parentElement;
    if (!parent) return false;

    this.renderer.addClass(this.wrapper, 'searchable-select');
    this.renderer.setStyle(this.wrapper, 'width', '100%');
    this.renderer.setStyle(this.wrapper, 'min-width', this.select.style.minWidth || '0');
    this.updateControlHeight();
    this.renderer.addClass(this.trigger, 'searchable-select-trigger');
    this.renderer.setAttribute(this.trigger, 'type', 'button');
    this.renderer.setAttribute(this.trigger, 'role', 'combobox');
    this.renderer.setAttribute(this.trigger, 'aria-haspopup', 'listbox');
    this.renderer.setAttribute(this.trigger, 'aria-expanded', 'false');
    this.renderer.setAttribute(this.trigger, 'aria-controls', this.id);
    this.renderer.addClass(this.valueText, 'searchable-select-value');
    this.renderer.appendChild(this.trigger, this.valueText);
    this.renderer.appendChild(this.wrapper, this.trigger);

    this.renderer.addClass(this.dropdown, 'searchable-select-dropdown');
    this.renderer.addClass(this.search, 'searchable-select-search');
    this.renderer.setAttribute(this.search, 'type', 'search');
    this.renderer.setAttribute(this.search, 'role', 'searchbox');
    this.renderer.setAttribute(this.search, 'placeholder', 'Search options');
    this.renderer.setAttribute(this.search, 'aria-label', 'Search options');
    this.renderer.addClass(this.optionsList, 'searchable-select-options');
    this.renderer.setAttribute(this.optionsList, 'id', this.id);
    this.renderer.setAttribute(this.optionsList, 'role', 'listbox');
    this.renderer.appendChild(this.dropdown, this.search);
    this.renderer.appendChild(this.dropdown, this.optionsList);

    this.renderer.insertBefore(parent, this.wrapper, this.select);
    this.renderer.appendChild(this.wrapper, this.select);
    this.renderer.setStyle(this.select, 'display', 'none');
    this.renderer.setAttribute(this.select, 'aria-hidden', 'true');
    this.renderer.setAttribute(this.select, 'tabindex', '-1');

    const ariaLabel = this.select.getAttribute('aria-label') ?? this.select.title;
    if (ariaLabel) this.renderer.setAttribute(this.trigger, 'aria-label', ariaLabel);

    this.unlisten.push(
      this.renderer.listen(this.trigger, 'click', () => this.toggle()),
      this.renderer.listen(this.trigger, 'keydown', (event: KeyboardEvent) => this.onTriggerKeydown(event)),
      this.renderer.listen(this.search, 'input', () => this.renderOptions()),
      this.renderer.listen(this.search, 'keydown', (event: KeyboardEvent) => this.onSearchKeydown(event)),
      this.renderer.listen(this.optionsList, 'click', (event: MouseEvent) => this.selectOption(event)),
      this.renderer.listen(this.optionsList, 'keydown', (event: KeyboardEvent) => this.onOptionKeydown(event)),
      this.renderer.listen('document', 'click', (event: MouseEvent) => this.onDocumentClick(event)),
      this.renderer.listen('window', 'resize', () => this.positionDropdown()),
      this.renderer.listen('window', 'scroll', () => this.positionDropdown(), { capture: true }),
    );
    this.observer.observe(this.select, { childList: true, subtree: true, attributes: true });

    this.destroyRef.onDestroy(() => {
      this.observer.disconnect();
      this.close();
      this.unlisten.forEach(unlisten => unlisten());
      const wrapperParent = this.wrapper.parentElement;
      if (wrapperParent) this.renderer.removeChild(wrapperParent, this.wrapper);
    });
    this.initialized = true;
    this.refresh();
    return true;
  }

  ngDoCheck(): void {
    if (this.select.multiple || (!this.initialized && !this.initialize())) return;
    this.ensureAttached();
    this.updateValue();
    this.updateControlHeight();
    this.updateDisabledState();
    this.updateAccessibility();
  }

  @HostListener('change')
  onChange(): void {
    this.updateValue();
  }

  private onTriggerKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.open();
      this.search.focus();
    } else if (event.key === 'Escape') {
      this.close();
    }
  }

  private refresh(): void {
    this.updateValue();
    this.updateDisabledState();
    this.updateAccessibility();
    if (this.isOpen) this.renderOptions();
  }

  private ensureAttached(): void {
    const parent = this.select.parentElement;
    if (!parent || parent === this.wrapper) return;

    this.renderer.insertBefore(parent, this.wrapper, this.select);
    this.renderer.appendChild(this.wrapper, this.select);
  }

  private updateValue(): void {
    const selected = this.select.selectedOptions.item(0);
    const label = selected?.textContent?.trim() ?? '';
    const value = `${this.select.value}|${label}`;
    if (value === this.renderedValue) return;
    this.renderedValue = value;
    this.renderer.setProperty(this.valueText, 'textContent', label);
  }

  private updateDisabledState(): void {
    this.renderer.setProperty(this.trigger, 'disabled', this.select.disabled);
  }

  private updateControlHeight(): void {
    const styles = getComputedStyle(this.select);
    const height = Math.max(
      this.select.getBoundingClientRect().height,
      Number.parseFloat(styles.height) || 0,
      Number.parseFloat(styles.minHeight) || 0,
    );
    if (height <= 0) return;

    const value = `${height}px`;
    if (value === this.renderedHeight) return;
    this.renderedHeight = value;
    this.renderer.setStyle(this.trigger, 'height', value);
  }

  private updateAccessibility(): void {
    const invalid = this.select.getAttribute('aria-invalid');
    if (invalid) this.renderer.setAttribute(this.trigger, 'aria-invalid', invalid);
    else this.renderer.removeAttribute(this.trigger, 'aria-invalid');

    const describedBy = this.select.getAttribute('aria-describedby');
    if (describedBy) this.renderer.setAttribute(this.trigger, 'aria-describedby', describedBy);
    else this.renderer.removeAttribute(this.trigger, 'aria-describedby');

    if (this.select.required) this.renderer.setAttribute(this.trigger, 'aria-required', 'true');
    else this.renderer.removeAttribute(this.trigger, 'aria-required');
  }

  private toggle(): void {
    if (this.select.disabled) return;
    if (this.isOpen) this.close();
    else this.open();
  }

  private open(): void {
    if (this.isOpen || this.select.disabled || this.select.multiple) return;
    this.isOpen = true;
    this.renderer.setAttribute(this.trigger, 'aria-expanded', 'true');
    this.renderer.appendChild(document.body, this.dropdown);
    this.renderer.setProperty(this.search, 'value', '');
    this.renderOptions();
    this.positionDropdown();
    this.search.focus();
  }

  private close(): void {
    if (!this.isOpen) return;
    this.isOpen = false;
    this.renderer.setAttribute(this.trigger, 'aria-expanded', 'false');
    const dropdownParent = this.dropdown.parentElement;
    if (dropdownParent) this.renderer.removeChild(dropdownParent, this.dropdown);
  }

  private positionDropdown(): void {
    if (!this.isOpen) return;
    const rect = this.trigger.getBoundingClientRect();
    this.renderer.setStyle(this.dropdown, 'left', `${rect.left}px`);
    this.renderer.setStyle(this.dropdown, 'width', `${rect.width}px`);
    const dropdownHeight = this.dropdown.getBoundingClientRect().height;
    const hasRoomBelow = window.innerHeight - rect.bottom >= dropdownHeight + 4;
    const top = !hasRoomBelow && rect.top >= dropdownHeight + 4
      ? rect.top - dropdownHeight - 4
      : rect.bottom + 4;
    this.renderer.setStyle(this.dropdown, 'top', `${top}px`);
  }

  private renderOptions(): void {
    this.renderer.setProperty(this.optionsList, 'textContent', '');
    const query = this.search.value.trim().toLocaleLowerCase();
    let visibleCount = 0;

    for (const option of Array.from(this.select.options) as HTMLOptionElement[]) {
      if (option.disabled) continue;
      const label = option.label.trim();
      if (!label.toLocaleLowerCase().includes(query)) continue;
      visibleCount += 1;
      const item = this.renderer.createElement('button') as HTMLButtonElement;
      this.renderer.addClass(item, 'searchable-select-option');
      this.renderer.setAttribute(item, 'type', 'button');
      this.renderer.setAttribute(item, 'role', 'option');
      this.renderer.setAttribute(item, 'data-value', option.value);
      this.renderer.setAttribute(item, 'aria-selected', option.selected ? 'true' : 'false');
      this.renderer.setProperty(item, 'textContent', label);
      this.renderer.appendChild(this.optionsList, item);
    }

    if (!visibleCount) {
      const empty = this.renderer.createElement('div');
      this.renderer.addClass(empty, 'searchable-select-empty');
      this.renderer.setProperty(empty, 'textContent', 'No options found');
      this.renderer.appendChild(this.optionsList, empty);
    }
  }

  private selectOption(event: MouseEvent): void {
    const target = event.target;
    if (!(target instanceof HTMLButtonElement) || !target.classList.contains('searchable-select-option')) return;
    this.select.value = target.dataset['value'] ?? '';
    this.select.dispatchEvent(new Event('change', { bubbles: true }));
    this.close();
    this.trigger.focus();
  }

  private onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      this.trigger.focus();
      return;
    }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      const options = Array.from(this.optionsList.querySelectorAll<HTMLButtonElement>('.searchable-select-option'));
      if (!options.length) return;
      const activeIndex = options.indexOf(document.activeElement as HTMLButtonElement);
      const nextIndex = event.key === 'ArrowDown'
        ? activeIndex < 0 ? 0 : Math.min(activeIndex + 1, options.length - 1)
        : activeIndex < 0 ? options.length - 1 : Math.max(activeIndex - 1, 0);
      options[nextIndex]?.focus();
    }
  }

  private onOptionKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      this.trigger.focus();
      return;
    }
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    event.preventDefault();
    const options = Array.from(this.optionsList.querySelectorAll<HTMLButtonElement>('.searchable-select-option'));
    if (!options.length) return;
    const current = options.indexOf(document.activeElement as HTMLButtonElement);
    const next = event.key === 'ArrowDown'
      ? Math.min(current + 1, options.length - 1)
      : Math.max(current - 1, 0);
    options[next]?.focus();
  }

  private onDocumentClick(event: MouseEvent): void {
    const target = event.target;
    if (target instanceof Node && !this.wrapper.contains(target) && !this.dropdown.contains(target)) {
      this.close();
    }
  }
}

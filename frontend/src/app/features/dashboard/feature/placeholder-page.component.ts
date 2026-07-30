import { Component, input } from '@angular/core';

@Component({
  selector: 'app-placeholder-page',
  template: `
    <section class="placeholder-page">
      <p class="eyebrow">{{ eyebrow() }}</p>
      <h2>{{ title() }}</h2>
      <p>{{ summary() }}</p>
    </section>
  `,
  styles: [`
    .placeholder-page {
      max-width: 760px;
    }

    .eyebrow {
      margin: 0 0 8px;
      color: #52637a;
      font-size: 0.78rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    h2 {
      margin: 0 0 8px;
      color: #1d2733;
      font-size: 1.15rem;
    }

    p {
      margin: 0;
      color: #36465a;
      line-height: 1.5;
    }
  `],
})
export class PlaceholderPageComponent {
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly summary = input.required<string>();
}
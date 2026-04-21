import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CommuneResponse } from '../commune-api';
import { ReportApi, SubmitReportOutcome } from '../report-api';

@Component({
  selector: 'app-report-form',
  imports: [ReactiveFormsModule],
  templateUrl: './report-form.html',
  styleUrl: './report-form.scss',
})
export class ReportForm {
  @Input({ required: true }) commune!: CommuneResponse;
  @Input({ required: true }) latitude!: number;
  @Input({ required: true }) longitude!: number;
  @Output() readonly submitted = new EventEmitter<string>();

  private readonly fb = inject(FormBuilder);
  private readonly reportApi = inject(ReportApi);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
    contactEmail: ['', [Validators.required, Validators.email]],
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.reportApi
      .submit({
        latitude: this.latitude,
        longitude: this.longitude,
        communeInsee: this.commune.codeInsee,
        communeName: this.commune.name,
        description: this.form.value.description!,
        contactEmail: this.form.value.contactEmail!,
      })
      .subscribe((outcome: SubmitReportOutcome) => {
        this.submitting.set(false);
        switch (outcome.status) {
          case 'submitted':
            this.submitted.emit(outcome.reportId);
            break;
          case 'validationError':
            this.errorMessage.set('Données invalides. Vérifiez les champs et réessayez.');
            break;
          case 'serverError':
            this.errorMessage.set('Erreur serveur — réessayez dans quelques instants.');
            break;
        }
      });
  }
}

import { Component, EventEmitter, Input, OnDestroy, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CommuneResponse } from '../commune-api';
import { ReportApi, SubmitReportOutcome } from '../report-api';

const MAX_PHOTO_SIZE = 5 * 1024 * 1024;
const ALLOWED_PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector: 'app-report-form',
  imports: [ReactiveFormsModule],
  templateUrl: './report-form.html',
  styleUrl: './report-form.scss',
})
export class ReportForm implements OnDestroy {
  @Input({ required: true }) commune!: CommuneResponse;
  @Input({ required: true }) latitude!: number;
  @Input({ required: true }) longitude!: number;
  @Output() readonly submitted = new EventEmitter<string>();

  private readonly fb = inject(FormBuilder);
  private readonly reportApi = inject(ReportApi);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedPhoto = signal<File | null>(null);
  readonly photoPreviewUrl = signal<string | null>(null);
  readonly photoError = signal<string | null>(null);

  readonly form = this.fb.group({
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
    contactEmail: ['', [Validators.required, Validators.email]],
  });

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) return;

    if (file.size > MAX_PHOTO_SIZE) {
      this.photoError.set('La photo ne peut pas dépasser 5 Mo.');
      return;
    }

    if (!ALLOWED_PHOTO_TYPES.includes(file.type)) {
      this.photoError.set('Format non supporté. Utilisez JPEG, PNG ou WebP.');
      return;
    }

    this.revokePreview();
    this.photoError.set(null);
    this.selectedPhoto.set(file);
    this.photoPreviewUrl.set(URL.createObjectURL(file));
  }

  removePhoto(): void {
    this.revokePreview();
    this.selectedPhoto.set(null);
    this.photoPreviewUrl.set(null);
    this.photoError.set(null);
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.reportApi
      .submit(
        {
          latitude: this.latitude,
          longitude: this.longitude,
          communeInsee: this.commune.codeInsee,
          communeName: this.commune.name,
          description: this.form.value.description!,
          contactEmail: this.form.value.contactEmail!,
        },
        this.selectedPhoto(),
      )
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

  ngOnDestroy(): void {
    this.revokePreview();
  }

  private revokePreview(): void {
    const url = this.photoPreviewUrl();
    if (url) URL.revokeObjectURL(url);
  }
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { LanguageService } from '../../../../../core/language.service';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { TeacherFilterOption } from '../../../domain/teacher-filter-option.model';
import { RosterStore } from '../../../state/roster.store';
import { FailedRowsPanelComponent } from '../../components/failed-rows-panel/failed-rows-panel.component';
import { ImportResultBadgeComponent } from '../../components/import-result-badge/import-result-badge.component';
import { LastImportCardComponent } from '../../components/last-import-card/last-import-card.component';
import { RosterStatTileComponent } from '../../components/roster-stat-tile/roster-stat-tile.component';

const ALL_TEACHERS = 'all';

@Component({
    selector: 'app-roster-page',
    imports: [
        FormsModule,
        TranslocoPipe,
        ButtonModule,
        ProgressSpinnerModule,
        SelectButtonModule,
        TableModule,
        TagModule,
        FailedRowsPanelComponent,
        ImportResultBadgeComponent,
        LastImportCardComponent,
        RosterStatTileComponent,
    ],
    templateUrl: './roster.page.html',
    styleUrl: './roster.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RosterPage {
    protected readonly store = inject(RosterStore);
    private readonly language = inject(LanguageService);

    protected readonly allTeachers = ALL_TEACHERS;

    protected readonly filterOptions = computed<TeacherFilterOption[]>(() => [
        { id: ALL_TEACHERS, name: '' },
        ...this.store.teacherFilterOptions(),
    ]);

    protected readonly selectedFilter = computed(() => this.store.selectedTeacherId() ?? ALL_TEACHERS);

    protected readonly processedRows = computed(() => {
        const latest = this.store.latestImport();

        return latest ? latest.added + latest.updated + latest.deactivated + latest.failed : 0;
    });

    protected readonly importedAtLabel = computed(() => {
        const latest = this.store.latestImport();

        return latest ? formatInstantInJerusalem(latest.importedAtUtc, this.language.lang()) : '';
    });

    protected onFilterChange(value: string): void {
        this.store.selectTeacher(value === ALL_TEACHERS ? null : value);
    }

    protected async onFileSelected(fileInput: HTMLInputElement): Promise<void> {
        const file = fileInput.files?.[0];

        if (!file) {
            return;
        }

        await this.store.upload(file);
        fileInput.value = '';
    }
}

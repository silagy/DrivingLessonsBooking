import { Pipe, PipeTransform } from '@angular/core';
import { initials } from '../text/initials';

@Pipe({ name: 'initials' })
export class InitialsPipe implements PipeTransform {
    transform(name: string): string {
        return initials(name);
    }
}

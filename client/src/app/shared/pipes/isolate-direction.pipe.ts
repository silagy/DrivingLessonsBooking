import { Pipe, PipeTransform } from '@angular/core';
import { isolateDirection } from '../text/isolate-direction';

@Pipe({ name: 'isolateDirection' })
export class IsolateDirectionPipe implements PipeTransform {
    transform(text: string): string {
        return isolateDirection(text);
    }
}

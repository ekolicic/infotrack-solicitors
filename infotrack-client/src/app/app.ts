import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { SolicitorService } from './services/solicitor.service';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly service = inject(SolicitorService);
  

  ngOnInit(): void {
    this.service.getSolicitors(["London"], false).subscribe({
      next: results => {
        console.log('Fetched solicitors:', results);
      },
      error: err => {
        console.error('Error fetching solicitors:', err);
      },
    });
  }
}

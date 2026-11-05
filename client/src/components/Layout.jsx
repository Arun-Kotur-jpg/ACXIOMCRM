import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';

export default function Layout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="app-wrapper">
      <Sidebar
        isOpen={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
      />

      <div className="app-main">
        <Topbar
          onToggleSidebar={() => setSidebarOpen(prev => !prev)}
        />

        <main className="app-content">
          <Outlet />
        </main>

        <footer className="app-footer">
          <div>&copy; 2026 AcxiomCRM &bull; MCA Final Project Evaluation</div>
          <div>MERN Stack Architecture &bull; MongoDB 8.3, Express, React, Node.js</div>
        </footer>
      </div>
    </div>
  );
}


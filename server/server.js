require('dotenv').config({ path: require('path').resolve(__dirname, '.env') });
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const morgan = require('morgan');
const path = require('path');
const fs = require('fs');

const { connectDB } = require('./src/config/db');
const { errorHandler } = require('./src/middleware/errorHandler');

// Route files
const authRoutes = require('./src/routes/authRoutes');
const customerRoutes = require('./src/routes/customerRoutes');
const leadRoutes = require('./src/routes/leadRoutes');
const opportunityRoutes = require('./src/routes/opportunityRoutes');
const followUpRoutes = require('./src/routes/followUpRoutes');
const activityRoutes = require('./src/routes/activityRoutes');
const reportRoutes = require('./src/routes/reportRoutes');
const userRoutes = require('./src/routes/userRoutes');
const auditRoutes = require('./src/routes/auditRoutes');

const app = express();

// Security and utility middleware
app.use(helmet({
  contentSecurityPolicy: false // Allow inline scripts/styles for Chart.js and CDNs
}));
app.use(cors({
  origin: '*',
  credentials: true
}));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

if (process.env.NODE_ENV !== 'test') {
  app.use(morgan('dev'));
}

// Health check endpoint
app.get('/api/health', (req, res) => {
  res.status(200).json({
    status: 'online',
    timestamp: new Date(),
    service: 'AcxiomCRM Enterprise MERN API'
  });
});

// Mount API Routes
app.use('/api/auth', authRoutes);
app.use('/api/customers', customerRoutes);
app.use('/api/leads', leadRoutes);
app.use('/api/opportunities', opportunityRoutes);
app.use('/api/followups', followUpRoutes);
app.use('/api/activities', activityRoutes);
app.use('/api/reports', reportRoutes);
app.use('/api/users', userRoutes);
app.use('/api/audit', auditRoutes);

// Serve built React client in production / standalone mode
const clientDistPath = path.resolve(__dirname, '../client/dist');
if (fs.existsSync(clientDistPath)) {
  app.use(express.static(clientDistPath));
  app.get('*', (req, res) => {
    res.sendFile(path.join(clientDistPath, 'index.html'));
  });
} else {
  app.get('/', (req, res) => {
    res.send(`
      <div style="font-family: sans-serif; padding: 40px; text-align: center;">
        <h1 style="color: #0284c7;">AcxiomCRM MERN API is Running</h1>
        <p>The Express backend is live. To start the React client, run: <code>npm run dev</code> or <code>npm run build</code>.</p>
        <p>Health check: <a href="/api/health">/api/health</a></p>
      </div>
    `);
  });
}

// Error handling middleware
app.use(errorHandler);

const PORT = process.env.PORT || 5000;

// Connect to MongoDB and start server
connectDB().then(() => {
  app.listen(PORT, () => {
    console.log(`
========================================================
🚀 AcxiomCRM Enterprise MERN Server
📡 API URL: http://localhost:${PORT}
🌱 Database: MongoDB 8.3 / Mongoose
👥 Roles: Admin, Manager, Sales Executive
========================================================
`);
  });
}).catch(err => {
  console.error('Fatal initialization error:', err);
});


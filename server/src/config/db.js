const mongoose = require('mongoose');
const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const { seedDatabase } = require('./seedData');

let mongoProcess = null;

async function startLocalMongod() {
  const possiblePaths = [
    'C:\\Program Files\\MongoDB\\Server\\8.3\\bin\\mongod.exe',
    'C:\\Program Files\\MongoDB\\Server\\8.0\\bin\\mongod.exe',
    'C:\\Program Files\\MongoDB\\Server\\7.0\\bin\\mongod.exe',
    'mongod'
  ];

  let mongodBin = null;
  for (const p of possiblePaths) {
    if (fs.existsSync(p)) {
      mongodBin = p;
      break;
    }
  }

  if (!mongodBin) {
    return false;
  }

  const dbPath = path.resolve(__dirname, '../../../data/db');
  if (!fs.existsSync(dbPath)) {
    fs.mkdirSync(dbPath, { recursive: true });
  }

  console.log(`🚀 Starting local MongoDB instance from ${mongodBin}...`);
  mongoProcess = spawn(mongodBin, ['--dbpath', dbPath, '--bind_ip', '127.0.0.1', '--port', '27017'], {
    detached: false,
    stdio: 'ignore'
  });

  // Give mongod a brief moment to initialize
  await new Promise(resolve => setTimeout(resolve, 2500));
  return true;
}

async function connectDB() {
  const uri = process.env.MONGODB_URI || 'mongodb://127.0.0.1:27017/acxiomcrm';

  try {
    await mongoose.connect(uri, { serverSelectionTimeoutMS: 3000 });
    console.log(`✅ MongoDB Connected successfully: ${mongoose.connection.host}/${mongoose.connection.name}`);
    await seedDatabase();
  } catch (err) {
    console.warn(`⚠️ First connection attempt to MongoDB failed: ${err.message}`);
    console.log('🔄 Attempting to initialize local MongoDB server process...');

    const started = await startLocalMongod();
    if (started) {
      try {
        await mongoose.connect(uri, { serverSelectionTimeoutMS: 5000 });
        console.log(`✅ MongoDB Connected successfully after local start: ${mongoose.connection.host}/${mongoose.connection.name}`);
        await seedDatabase();
        return;
      } catch (retryErr) {
        console.error('❌ Could not connect to newly started MongoDB:', retryErr.message);
      }
    }

    console.error('❌ MongoDB Connection Error. Please verify your MongoDB service is running.');
    process.exit(1);
  }
}

// Cleanup mongod on exit if spawned
process.on('SIGINT', () => {
  if (mongoProcess) {
    try { mongoProcess.kill(); } catch (e) {}
  }
  process.exit(0);
});

module.exports = { connectDB };

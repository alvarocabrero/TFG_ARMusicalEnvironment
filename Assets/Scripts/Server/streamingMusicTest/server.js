/**
 * NPM Module dependencies.
 */
const express = require('express');
const trackRoute = express.Router();
const multer = require('multer');

const mongodb = require('mongodb');
const MongoClient = require('mongodb').MongoClient;
const ObjectID = require('mongodb').ObjectID;


/**
 * NodeJS Module dependencies.
 */
const { Readable } = require('stream');

/**
 * Create Express server && Express Router configuration.
 */
const app = express();
app.use('/tracks', trackRoute);



/**
 * Connect Mongo Driver to MongoDB.
 */
let db;
MongoClient.connect('mongodb://localhost/trackDB',(err, database) => {
  if (err) {
    console.log('MongoDB Connection Error. Please make sure that MongoDB is running.');
    process.exit(1);
  }
  db = database;
});

trackRoute.get('/collections', (req,res) =>{
        res.set('content-type', 'application/json');
        db.collection("tracks.files").find({}).toArray(function (err, result) {
          if (err) throw err;
            console.log(result);
          res.json(result);
        });
});


/**
 * GET /tracks/:trackID
 */
trackRoute.get('/obtain/:trackID', (req, res) => {
  try {
  
    var trackID = new ObjectID(req.params.trackID);
    
  } catch(err) {
    return res.status(400).json({ message: "Invalid trackID in URL parameter. Must be a single String of 12 bytes or a string of 24 hex characters" }); 
  }
  res.set('content-type', 'audio/x-wav');
  res.set('accept-ranges', 'bytes');

  let bucket = new mongodb.GridFSBucket(db, {
    bucketName: 'tracks'
  });
  let downloadStream = bucket.openDownloadStream(trackID);

  downloadStream.on('data', (chunk) => {
    res.write(chunk);
  });

  downloadStream.on('error', () => {
    res.sendStatus(404);
  });

  downloadStream.on('end', () => {
    res.end();
  });
});




/*
*   Get Remove
*/
trackRoute.get('/remove/:nombreSesion', (req, res) => {
  var myquery = {"metadata.nombreSesion": req.params.nombreSesion};
   console.log(req.params.nombreSesion);
  db.collection("tracks.files").deleteMany(myquery, function(err, obj) {
    if (err) res.send( err);
    res.send( obj.result.n + " document(s) deleted");   
  });
});

/**
 * POST /tracks
 */
trackRoute.post('/', (req, res) => {
  const storage = multer.memoryStorage()
  const upload = multer({ storage: storage, limits: { fields: 5, files: 1, parts: 6 }});

  upload.single('track')(req, res, (err) => {
    if (err) {
      return res.status(400).json({ message: "Upload Request Validation Failed" + err });
    } else if(!req.body.name) {
      return res.status(400).json({ message: "No track name in request body" });
    }
    
    let trackName = req.body.name;
    
    // Covert buffer to Readable Stream
    const readableTrackStream = new Readable();
    readableTrackStream.push(req.file.buffer);
    readableTrackStream.push(null);

    let bucket = new mongodb.GridFSBucket(db, {
      bucketName: 'tracks'
      
    });
  
    let uploadStream = bucket.openUploadStream(trackName, {metadata:{nombreImagen:req.body.nombreImagen, figura:req.body.figura, nombreSesion:req.body.nombreSesion, bpmSession:req.body.bpmSesion}});
    let id = uploadStream.id;
    readableTrackStream.pipe(uploadStream);

    uploadStream.on('error', () => {
      return res.status(500).json({ message: "Error uploading file" });
    });

    uploadStream.on('finish', () => {
    return res.status(201).json(id);
 });
  
  
 });

  
});

app.listen('3005', '192.168.0.15', () => {
  console.log("App listening on port 3005!");
});
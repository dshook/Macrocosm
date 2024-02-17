var restify = require('restify');
var exec = require('child_process').exec;

//Webhook that gets hit when the unity cloud build finishes and then runs the testflight upload
const server = restify.createServer({
  name: 'Cloud Build Webhook',
  version: '1.0.0'
});

server.use(restify.plugins.acceptParser(server.acceptable));
server.use(restify.plugins.queryParser());
server.use(restify.plugins.bodyParser());

server.get('/', function (req, res, next) {
  res.send('Hello Macrocosm');
  return next();
});

server.post('/webhook', function(req, res, next) {
  console.log('Hit webhook ' + new Date(), {
    params: req.params,
    body: req.body
  });

  if(req.body.platform !== 'ios'){
    console.log('Non ios build, skipping');
    return next();
  }

  try{
    dir = exec("./testflight-upload.sh", function(err, stdout, stderr) {
      console.log(stdout);
      res.send(stdout);
      if(stderr){
        console.error(stderr);
        if(!res.headersSent){
          res.status(500).send('Error');
        }
      }
    });
  }catch(e){
    console.error('Other exception', e);
    if(!res.headersSent){
      res.status(500).send('Error');
    }
  }
  return next();
});

server.listen(7777, function () {
  console.log('%s listening at %s', server.name, server.url);
});
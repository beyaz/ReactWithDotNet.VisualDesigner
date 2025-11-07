const express = require('express');
const prettier = require('prettier');

const app = express();
const port = 5009;

app.use(express.json({ limit: '4mb' }));

app.post('/format', async (req, res) => 
{
  const code = req.body.code;
  const options = req.body.options || {}

  if (!code) 
  {
    return res.status(400).json({ error: "EmptyRequest" });
  }

  try
  {
    
    const formattedCode = await prettier.format(code, { 
      parser: 'typescript',
      semi: true,
      singleQuote: false,
      trailingComma: 'es5',
      arrowParens: 'avoid',
      useTabs: false,
      tabWidth: 2,
      ...options
    });

    res.status(200).json({ formattedCode });
  } 
  catch (err) 
  {
    res.status(500).json({ error: "FormattingErrorOccurred", details: err.message });
  }
});

app.listen(port, () => 
{
  console.log(`Started- http://localhost:${port}`);
});
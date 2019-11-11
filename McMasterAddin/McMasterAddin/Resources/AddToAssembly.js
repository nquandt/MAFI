document.
$('.InLnOrdWebPart_TransInfo').replaceWith('<div id="partNumber{pnReplace}"></div>');
var node = document.getElementById('partNumber{pnReplace}');
var newButton = document.createElement('button');
newButton.className = 'button-add-to-order-inline add-to-order customButton';
newButton.style = 'font-size:11px;';
var newSpan = document.createElement('span');
newSpan.className = 'button-reset--IE';
newSpan.innerText = 'add to assembly';
newButton.appendChild(newSpan);
newButton.onclick = function () { callAddToAssembly(); };
node.appendChild(newButton);
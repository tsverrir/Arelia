window.areliaInterop = window.areliaInterop || {};

window.areliaInterop.setDocumentSkin = (skinKey) => {
    document.documentElement.setAttribute("data-arelia-skin", skinKey);
};

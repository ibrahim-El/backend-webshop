﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using bestelling_model;
using klant_model;
using bestellingproduct_model;
using product_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bestelling_Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class bestellingController : ControllerBase
    {

        private readonly kamerplantContext _context;

        public bestellingController(kamerplantContext context)
        {
            _context = context;
        }


        // GET api/admin
        [HttpGet]
        public List<bestelling> Get()
        {
            return _context.bestelling.ToList();
        }

        // GET api/bestelling/5
        [HttpGet("{id}")]
        public bestelling Get(int id)
        {
                return _context.bestelling.Find(id);
        }

        //Definitie van te ontvangen order object
        public class Order
        {
            public int klantID { get; set; }
            public int[] producten { get; set; } //Product ID's
            public bool geregistreerd { get; set; }
        }

        // POST api/bestelling
        [HttpPost]
        public StatusCodeResult Post([FromBody] Order order)
        {
            //Boolean to determine if order can be placed
            bool allProductsInStock = true;

            //Bestelling
            bestelling newBestelling = new bestelling();
            newBestelling.datum = DateTime.Now.ToString();
            newBestelling.prijs = 0.00;
            newBestelling.klantID = order.klantID;
            newBestelling.geregistreerd = order.geregistreerd;

            //Bestelling opbouwen uit aangeleverde json
            //Bestellingproduct krijgen wij binnen als array product ID's
            for(int i = 0;i < order.producten.Length; i++)
            {
                //Check of product in voorraad is.
                product Product = _context.product.Find(order.producten[i]);
                bool productInVoorraad = (Product.voorraad > 0) ? true : false;

                if (productInVoorraad)
                {
                    //Product voorraad met 1 verminderen
                    Product.voorraad -= 1;

                    //bestellingproduct relatie opbouw:
                    bestellingproduct productCombinatie = new bestellingproduct();
                    productCombinatie.productID = order.producten[i];
                    productCombinatie.bestellingID = newBestelling.ID;
                    productCombinatie.product = Product;
                    productCombinatie.bestelling = newBestelling;
                    productCombinatie.verkoopPrijs = _context.product.Find(order.producten[i]).prijs;
                    _context.bestellingproduct.Add(productCombinatie);
                    newBestelling.producten.Append(productCombinatie);
                    newBestelling.prijs = (newBestelling.prijs + productCombinatie.verkoopPrijs);
                } 
                else 
                {
                    allProductsInStock = false;
                }
            }

            if(allProductsInStock)
            {
                try
                {
                    _context.bestelling.Add(newBestelling);
                    _context.SaveChanges();
                    return Ok();
                } 
                catch 
                {
                    return BadRequest();
                }
            }
            else
            {
                return StatusCode(410); //Een van de producten niet in voorrraad
            }

        }
    }
}